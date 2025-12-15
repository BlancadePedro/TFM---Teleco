#!/usr/bin/env python3
"""
Entrena TODOS los modelos (Transformer, GRU, TCN) y genera comparativa completa
"""

import os
import json
import pickle
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import Dataset, DataLoader
from torch.nn.utils.rnn import pad_sequence
from pathlib import Path
from tqdm import tqdm
import argparse

from asl_classifier import (
    ASLClassifierTransformer,
    ASLClassifierGRU,
    ASLClassifierTCN
)


class ASLSequenceDataset(Dataset):
    """Dataset para secuencias de landmarks ASL"""
    def __init__(self, data_path):
        with open(data_path, 'rb') as f:
            data = pickle.load(f)
        self.samples = data['samples']

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        sample = self.samples[idx]
        landmarks = torch.FloatTensor(sample['landmarks'])
        landmarks = landmarks.reshape(len(landmarks), -1)
        label = torch.LongTensor([sample['label']])[0]
        seq_length = len(landmarks)
        return landmarks, label, seq_length


def collate_sequences(batch):
    """Padding de secuencias de diferentes longitudes"""
    sequences, labels, lengths = zip(*batch)
    padded_sequences = pad_sequence(sequences, batch_first=True, padding_value=0.0)
    labels = torch.stack(labels)
    lengths = torch.LongTensor(lengths)
    return padded_sequences, labels, lengths


class MultiModelTrainer:
    def __init__(self, data_dir, output_dir, device='cpu'):
        self.data_dir = Path(data_dir)
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.device = torch.device(device)

        # Cargar metadata
        with open(self.data_dir / 'metadata.json', 'r') as f:
            self.metadata = json.load(f)
        self.num_classes = self.metadata['num_classes']

        # Cargar datasets
        self.train_dataset = ASLSequenceDataset(self.data_dir / 'train.pkl')
        self.val_dataset = ASLSequenceDataset(self.data_dir / 'val.pkl')
        self.test_dataset = ASLSequenceDataset(self.data_dir / 'test.pkl')

        print(f"Datasets cargados: Train={len(self.train_dataset)}, "
              f"Val={len(self.val_dataset)}, Test={len(self.test_dataset)}")

    def train_model(self, model, model_name, epochs=100, batch_size=32,
                   lr=0.001, patience=15):
        """Entrena un modelo específico"""
        print(f"\n{'='*80}")
        print(f"ENTRENANDO {model_name.upper()}")
        print(f"{'='*80}")

        model = model.to(self.device)
        print(f"Parámetros: {sum(p.numel() for p in model.parameters()):,}")

        # Dataloaders
        train_loader = DataLoader(
            self.train_dataset, batch_size=batch_size, shuffle=True,
            num_workers=0, collate_fn=collate_sequences
        )
        val_loader = DataLoader(
            self.val_dataset, batch_size=batch_size, shuffle=False,
            num_workers=0, collate_fn=collate_sequences
        )

        # Training setup
        criterion = nn.CrossEntropyLoss()
        optimizer = optim.Adam(model.parameters(), lr=lr)
        scheduler = optim.lr_scheduler.ReduceLROnPlateau(
            optimizer, mode='max', factor=0.5, patience=5
        )

        history = {'train_loss': [], 'train_acc': [], 'val_loss': [], 'val_acc': []}
        best_val_acc = 0
        epochs_without_improvement = 0

        for epoch in range(epochs):
            # Train
            model.train()
            train_loss = 0
            train_correct = 0
            train_total = 0

            for sequences, labels, lengths in tqdm(train_loader,
                                                  desc=f"Epoch {epoch+1}/{epochs}",
                                                  leave=False):
                sequences, labels = sequences.to(self.device), labels.to(self.device)
                optimizer.zero_grad()
                outputs = model(sequences)
                loss = criterion(outputs, labels)
                loss.backward()
                optimizer.step()

                train_loss += loss.item() * sequences.size(0)
                _, predicted = outputs.max(1)
                train_total += labels.size(0)
                train_correct += predicted.eq(labels).sum().item()

            train_loss /= train_total
            train_acc = 100. * train_correct / train_total

            # Validation
            model.eval()
            val_loss = 0
            val_correct = 0
            val_total = 0

            with torch.no_grad():
                for sequences, labels, lengths in val_loader:
                    sequences, labels = sequences.to(self.device), labels.to(self.device)
                    outputs = model(sequences)
                    loss = criterion(outputs, labels)
                    val_loss += loss.item() * sequences.size(0)
                    _, predicted = outputs.max(1)
                    val_total += labels.size(0)
                    val_correct += predicted.eq(labels).sum().item()

            val_loss /= val_total
            val_acc = 100. * val_correct / val_total

            history['train_loss'].append(train_loss)
            history['train_acc'].append(train_acc)
            history['val_loss'].append(val_loss)
            history['val_acc'].append(val_acc)

            print(f"Epoch {epoch+1}: Loss={train_loss:.4f}, Acc={train_acc:.2f}%, "
                  f"Val Loss={val_loss:.4f}, Val Acc={val_acc:.2f}%")

            scheduler.step(val_acc)

            if val_acc > best_val_acc:
                best_val_acc = val_acc
                epochs_without_improvement = 0
                torch.save({
                    'epoch': epoch,
                    'model_state_dict': model.state_dict(),
                    'val_acc': val_acc,
                    'model_name': model_name
                }, self.output_dir / f'best_model_{model_name}.pth')
                print(f"  -> Mejor modelo guardado (Val Acc: {val_acc:.2f}%)")
            else:
                epochs_without_improvement += 1
                if epochs_without_improvement >= patience:
                    print(f"Early stopping en epoch {epoch+1}")
                    break

        # Guardar historial
        with open(self.output_dir / f'history_{model_name}.json', 'w') as f:
            json.dump(history, f)

        return model, history, best_val_acc

    def evaluate_model(self, model, model_name):
        """Evalúa modelo en test set"""
        checkpoint = torch.load(self.output_dir / f'best_model_{model_name}.pth',
                               map_location=self.device)
        model.load_state_dict(checkpoint['model_state_dict'])
        model.eval()

        test_loader = DataLoader(
            self.test_dataset, batch_size=32, shuffle=False,
            num_workers=0, collate_fn=collate_sequences
        )

        all_preds = []
        all_labels = []

        with torch.no_grad():
            for sequences, labels, lengths in test_loader:
                sequences = sequences.to(self.device)
                outputs = model(sequences)
                _, predicted = outputs.max(1)
                all_preds.extend(predicted.cpu().numpy())
                all_labels.extend(labels.numpy())

        accuracy = (np.array(all_preds) == np.array(all_labels)).mean() * 100

        print(f"\n{model_name.upper()} Test Accuracy: {accuracy:.2f}%")
        return accuracy

    def run_all(self):
        """Entrena y evalúa todos los modelos"""
        results = {}

        # 1. Transformer
        print("\n" + "="*80)
        print("1/3: TRANSFORMER")
        print("="*80)
        transformer = ASLClassifierTransformer(
            num_classes=self.num_classes,
            input_size=63,
            d_model=128,
            nhead=4,
            num_layers=2,
            dropout=0.3
        )
        _, hist_trans, best_trans = self.train_model(transformer, 'transformer')
        test_trans = self.evaluate_model(transformer, 'transformer')
        results['transformer'] = {
            'best_val_acc': best_trans,
            'test_acc': test_trans,
            'params': sum(p.numel() for p in transformer.parameters())
        }

        # 2. GRU
        print("\n" + "="*80)
        print("2/3: GRU")
        print("="*80)
        gru = ASLClassifierGRU(
            num_classes=self.num_classes,
            input_size=63,
            hidden_size=128,
            num_layers=2,
            dropout=0.3
        )
        _, hist_gru, best_gru = self.train_model(gru, 'gru')
        test_gru = self.evaluate_model(gru, 'gru')
        results['gru'] = {
            'best_val_acc': best_gru,
            'test_acc': test_gru,
            'params': sum(p.numel() for p in gru.parameters())
        }

        # 3. TCN
        print("\n" + "="*80)
        print("3/3: TCN")
        print("="*80)
        tcn = ASLClassifierTCN(
            num_classes=self.num_classes,
            input_size=63,
            num_channels=[64, 128, 128, 64],
            dropout=0.3
        )
        _, hist_tcn, best_tcn = self.train_model(tcn, 'tcn')
        test_tcn = self.evaluate_model(tcn, 'tcn')
        results['tcn'] = {
            'best_val_acc': best_tcn,
            'test_acc': test_tcn,
            'params': sum(p.numel() for p in tcn.parameters())
        }

        # Guardar resultados
        with open(self.output_dir / 'all_models_results.json', 'w') as f:
            json.dump(results, f, indent=2)

        # Resumen
        print("\n" + "="*80)
        print("RESUMEN FINAL - TODOS LOS MODELOS")
        print("="*80)
        print(f"{'Modelo':<15} {'Params':<12} {'Val Acc':<10} {'Test Acc':<10}")
        print("-"*80)
        for model_name, res in results.items():
            print(f"{model_name.upper():<15} {res['params']:<12,} "
                  f"{res['best_val_acc']:<10.2f} {res['test_acc']:<10.2f}")

        return results


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--data_dir', type=str,
                       default='/fs/nexus-scratch/bdepedro/TFM_ASL_VR/data/processed/sequence_splits')
    parser.add_argument('--output_dir', type=str,
                       default='/fs/nexus-scratch/bdepedro/TFM_ASL_VR/outputs/asl_model')
    parser.add_argument('--device', type=str, default='cpu')
    args = parser.parse_args()

    trainer = MultiModelTrainer(args.data_dir, args.output_dir, args.device)
    results = trainer.run_all()

    print("\n✓ Entrenamiento de todos los modelos completado")


if __name__ == "__main__":
    main()
