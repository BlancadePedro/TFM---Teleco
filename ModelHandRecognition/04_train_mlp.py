#!/usr/bin/env python3
"""
Script de entrenamiento para el clasificador ASL
"""

import os
import json
import pickle
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import Dataset, DataLoader
from pathlib import Path
from tqdm import tqdm
import argparse

from asl_classifier import ASLClassifier, export_to_onnx


class ASLDataset(Dataset):
    """Dataset para landmarks ASL"""
    def __init__(self, data_path):
        with open(data_path, 'rb') as f:
            data = pickle.load(f)

        self.X = torch.FloatTensor(data['X'])
        self.y = torch.LongTensor(data['y'])

    def __len__(self):
        return len(self.y)

    def __getitem__(self, idx):
        return self.X[idx], self.y[idx]


class Trainer:
    def __init__(self, data_dir, output_dir, device='cuda'):
        self.data_dir = Path(data_dir)
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)

        # Device
        self.device = torch.device(device if torch.cuda.is_available() else 'cpu')
        print(f"Usando dispositivo: {self.device}")

        # Cargar metadata
        with open(self.data_dir / 'metadata.json', 'r') as f:
            self.metadata = json.load(f)

        self.num_classes = self.metadata['num_classes']
        self.num_features = self.metadata['num_features']

        # Cargar datasets
        self.train_dataset = ASLDataset(self.data_dir / 'train.pkl')
        self.val_dataset = ASLDataset(self.data_dir / 'val.pkl')
        self.test_dataset = ASLDataset(self.data_dir / 'test.pkl')

        print(f"Datasets cargados:")
        print(f"  Train: {len(self.train_dataset)}")
        print(f"  Val: {len(self.val_dataset)}")
        print(f"  Test: {len(self.test_dataset)}")

    def train(self, epochs=100, batch_size=32, lr=0.001, hidden_sizes=[256, 128, 64],
              dropout=0.3, patience=15):
        """Entrena el modelo"""
        # Crear dataloaders
        num_workers = 0 if self.device.type == 'cpu' else 4
        train_loader = DataLoader(
            self.train_dataset, batch_size=batch_size, shuffle=True, num_workers=num_workers
        )
        val_loader = DataLoader(
            self.val_dataset, batch_size=batch_size, shuffle=False, num_workers=num_workers
        )

        # Crear modelo
        model = ASLClassifier(
            num_classes=self.num_classes,
            input_size=self.num_features,
            hidden_sizes=hidden_sizes,
            dropout=dropout
        ).to(self.device)

        print(f"\nModelo creado:")
        print(f"  Parámetros: {sum(p.numel() for p in model.parameters()):,}")
        print(f"  Hidden sizes: {hidden_sizes}")
        print(f"  Dropout: {dropout}")

        # Criterio y optimizador
        criterion = nn.CrossEntropyLoss()
        optimizer = optim.Adam(model.parameters(), lr=lr)
        scheduler = optim.lr_scheduler.ReduceLROnPlateau(
            optimizer, mode='max', factor=0.5, patience=5
        )

        # Historial
        history = {
            'train_loss': [], 'train_acc': [],
            'val_loss': [], 'val_acc': []
        }

        best_val_acc = 0
        epochs_without_improvement = 0

        print(f"\nIniciando entrenamiento...")
        print(f"  Epochs: {epochs}")
        print(f"  Batch size: {batch_size}")
        print(f"  Learning rate: {lr}")
        print(f"  Patience: {patience}")
        print()

        for epoch in range(epochs):
            # Training
            model.train()
            train_loss = 0
            train_correct = 0
            train_total = 0

            for X_batch, y_batch in tqdm(train_loader, desc=f"Epoch {epoch+1}/{epochs}", leave=False):
                X_batch, y_batch = X_batch.to(self.device), y_batch.to(self.device)

                optimizer.zero_grad()
                outputs = model(X_batch)
                loss = criterion(outputs, y_batch)
                loss.backward()
                optimizer.step()

                train_loss += loss.item() * X_batch.size(0)
                _, predicted = outputs.max(1)
                train_total += y_batch.size(0)
                train_correct += predicted.eq(y_batch).sum().item()

            train_loss /= train_total
            train_acc = 100. * train_correct / train_total

            # Validation
            model.eval()
            val_loss = 0
            val_correct = 0
            val_total = 0

            with torch.no_grad():
                for X_batch, y_batch in val_loader:
                    X_batch, y_batch = X_batch.to(self.device), y_batch.to(self.device)

                    outputs = model(X_batch)
                    loss = criterion(outputs, y_batch)

                    val_loss += loss.item() * X_batch.size(0)
                    _, predicted = outputs.max(1)
                    val_total += y_batch.size(0)
                    val_correct += predicted.eq(y_batch).sum().item()

            val_loss /= val_total
            val_acc = 100. * val_correct / val_total

            # Guardar historial
            history['train_loss'].append(train_loss)
            history['train_acc'].append(train_acc)
            history['val_loss'].append(val_loss)
            history['val_acc'].append(val_acc)

            # Print
            print(f"Epoch {epoch+1}/{epochs} - "
                  f"Loss: {train_loss:.4f} - Acc: {train_acc:.2f}% - "
                  f"Val Loss: {val_loss:.4f} - Val Acc: {val_acc:.2f}%")

            # Scheduler
            scheduler.step(val_acc)

            # Early stopping
            if val_acc > best_val_acc:
                best_val_acc = val_acc
                epochs_without_improvement = 0

                # Guardar mejor modelo
                torch.save({
                    'epoch': epoch,
                    'model_state_dict': model.state_dict(),
                    'optimizer_state_dict': optimizer.state_dict(),
                    'val_acc': val_acc,
                    'hidden_sizes': hidden_sizes
                }, self.output_dir / 'best_model.pth')

                print(f"  -> Nuevo mejor modelo guardado (Val Acc: {val_acc:.2f}%)")
            else:
                epochs_without_improvement += 1
                if epochs_without_improvement >= patience:
                    print(f"\nEarly stopping en epoch {epoch+1}")
                    break

        # Guardar historial
        with open(self.output_dir / 'history.json', 'w') as f:
            json.dump(history, f)

        return model, history, best_val_acc

    def evaluate(self):
        """Evalúa el modelo en el conjunto de test"""
        # Cargar mejor modelo
        checkpoint = torch.load(self.output_dir / 'best_model.pth')
        hidden_sizes = checkpoint.get('hidden_sizes', [256, 128, 64])

        model = ASLClassifier(
            num_classes=self.num_classes,
            input_size=self.num_features,
            hidden_sizes=hidden_sizes
        ).to(self.device)

        model.load_state_dict(checkpoint['model_state_dict'])
        model.eval()

        num_workers = 0 if self.device.type == 'cpu' else 4
        test_loader = DataLoader(
            self.test_dataset, batch_size=32, shuffle=False, num_workers=num_workers
        )

        all_preds = []
        all_labels = []

        with torch.no_grad():
            for X_batch, y_batch in test_loader:
                X_batch = X_batch.to(self.device)
                outputs = model(X_batch)
                _, predicted = outputs.max(1)

                all_preds.extend(predicted.cpu().numpy())
                all_labels.extend(y_batch.numpy())

        # Calcular métricas
        all_preds = np.array(all_preds)
        all_labels = np.array(all_labels)

        accuracy = (all_preds == all_labels).mean() * 100

        print(f"\n" + "=" * 60)
        print("EVALUACIÓN EN TEST SET")
        print("=" * 60)
        print(f"Accuracy: {accuracy:.2f}%")

        # Accuracy por clase
        idx_to_label = self.metadata['idx_to_label']
        print("\nAccuracy por clase:")

        class_correct = {}
        class_total = {}

        for pred, label in zip(all_preds, all_labels):
            label_name = idx_to_label[str(label)]
            if label_name not in class_total:
                class_total[label_name] = 0
                class_correct[label_name] = 0

            class_total[label_name] += 1
            if pred == label:
                class_correct[label_name] += 1

        for label_name in sorted(class_total.keys()):
            acc = 100. * class_correct[label_name] / class_total[label_name]
            print(f"  {label_name}: {acc:.1f}% ({class_correct[label_name]}/{class_total[label_name]})")

        return accuracy

    def export_onnx(self):
        """Exporta el modelo a ONNX"""
        # Cargar mejor modelo
        checkpoint = torch.load(self.output_dir / 'best_model.pth')
        hidden_sizes = checkpoint.get('hidden_sizes', [256, 128, 64])

        model = ASLClassifier(
            num_classes=self.num_classes,
            input_size=self.num_features,
            hidden_sizes=hidden_sizes
        )

        model.load_state_dict(checkpoint['model_state_dict'])

        onnx_path = self.output_dir / 'asl_classifier.onnx'
        export_to_onnx(model, str(onnx_path), self.num_features)

        return onnx_path


def main():
    parser = argparse.ArgumentParser(description='Entrenar clasificador ASL')
    parser.add_argument('--data_dir', type=str,
                       default='/fs/nexus-scratch/bdepedro/TFM_ASL_VR/data/processed/splits',
                       help='Directorio con los splits')
    parser.add_argument('--output_dir', type=str,
                       default='/fs/nexus-scratch/bdepedro/TFM_ASL_VR/outputs/asl_model',
                       help='Directorio de salida')
    parser.add_argument('--epochs', type=int, default=100)
    parser.add_argument('--batch_size', type=int, default=32)
    parser.add_argument('--lr', type=float, default=0.001)
    parser.add_argument('--dropout', type=float, default=0.3)
    parser.add_argument('--patience', type=int, default=15)
    parser.add_argument('--device', type=str, default='cuda')

    args = parser.parse_args()

    trainer = Trainer(
        data_dir=args.data_dir,
        output_dir=args.output_dir,
        device=args.device
    )

    # Entrenar
    model, history, best_val_acc = trainer.train(
        epochs=args.epochs,
        batch_size=args.batch_size,
        lr=args.lr,
        dropout=args.dropout,
        patience=args.patience
    )

    # Evaluar
    test_acc = trainer.evaluate()

    # Exportar a ONNX
    onnx_path = trainer.export_onnx()

    print(f"\n" + "=" * 60)
    print("ENTRENAMIENTO COMPLETADO")
    print("=" * 60)
    print(f"Mejor Val Accuracy: {best_val_acc:.2f}%")
    print(f"Test Accuracy: {test_acc:.2f}%")
    print(f"Modelo ONNX: {onnx_path}")


if __name__ == "__main__":
    main()
