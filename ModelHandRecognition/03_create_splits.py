#!/usr/bin/env python3
"""
Limpia datos WLASL y prepara dataset unificado con solo Kaggle
- Elimina todos los datos WLASL
- Mantiene solo alfabeto (a-z) y números (zero-nine) de Kaggle
- Crea splits train/val/test
"""

import os
import pickle
import shutil
import numpy as np
from pathlib import Path
from collections import defaultdict
import json
from sklearn.model_selection import train_test_split

# Clases objetivo: solo alfabeto y números
TARGET_CLASSES = (
    list('abcdefghijklmnopqrstuvwxyz') +
    ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine']
)

class DatasetCleaner:
    def __init__(self, data_dir: str):
        self.data_dir = Path(data_dir)
        self.processed_dir = self.data_dir / 'processed/all'
        self.output_dir = self.data_dir / 'processed/kaggle_only'

    def skip_cleaning(self):
        """Salta el paso de limpieza (ya se hizo con process_full_kaggle.py)"""
        print("=" * 60)
        print("USANDO DATASET KAGGLE YA PROCESADO")
        print("=" * 60)

        stats = {
            'kept_classes': [],
            'kept_samples': 0
        }

        for sign_dir in sorted(self.output_dir.iterdir()):
            if not sign_dir.is_dir():
                continue

            gloss = sign_dir.name.lower()
            samples = list(sign_dir.glob('*.pkl'))
            stats['kept_classes'].append(gloss)
            stats['kept_samples'] += len(samples)
            print(f"  {gloss}: {len(samples)} muestras")

        print(f"\nTotal: {len(stats['kept_classes'])} clases, {stats['kept_samples']} muestras")

        return stats

    def create_splits(self, train_ratio=0.70, val_ratio=0.15, test_ratio=0.15,
                      random_state=42):
        """Crea splits train/val/test estratificados"""
        print("\n" + "=" * 60)
        print("CREANDO SPLITS TRAIN/VAL/TEST")
        print("=" * 60)

        # Recopilar todos los datos
        all_samples = []
        all_labels = []

        # Crear mapeo de etiquetas a índices
        label_to_idx = {label: idx for idx, label in enumerate(sorted(TARGET_CLASSES))}

        for sign_dir in sorted(self.output_dir.iterdir()):
            if not sign_dir.is_dir():
                continue

            gloss = sign_dir.name.lower()
            if gloss not in label_to_idx:
                continue

            for pkl_file in sign_dir.glob('*.pkl'):
                try:
                    with open(pkl_file, 'rb') as f:
                        data = pickle.load(f)

                    # Extraer landmarks
                    landmarks = data['landmarks']

                    # Normalizar a single frame si es necesario
                    if len(landmarks.shape) == 3:
                        # (frames, 21, 3) -> usar primer frame
                        landmarks = landmarks[0]

                    # Flatten a vector
                    features = landmarks.flatten()  # 21 * 3 = 63 valores

                    all_samples.append({
                        'features': features,
                        'landmarks': landmarks,
                        'label': label_to_idx[gloss],
                        'gloss': gloss,
                        'file': pkl_file.name,
                        'source': data.get('source', 'unknown')
                    })
                    all_labels.append(label_to_idx[gloss])

                except Exception as e:
                    print(f"  Error cargando {pkl_file}: {e}")

        print(f"\nTotal muestras cargadas: {len(all_samples)}")
        print(f"Total clases: {len(set(all_labels))}")

        # Verificar distribución mínima para split
        label_counts = defaultdict(int)
        for label in all_labels:
            label_counts[label] += 1

        min_count = min(label_counts.values())
        print(f"Mínimo muestras por clase: {min_count}")

        # Crear splits
        # Primero dividir en train+val y test
        X_indices = list(range(len(all_samples)))

        # Stratified split
        train_val_idx, test_idx = train_test_split(
            X_indices,
            test_size=test_ratio,
            stratify=all_labels,
            random_state=random_state
        )

        # Luego dividir train+val en train y val
        train_val_labels = [all_labels[i] for i in train_val_idx]
        val_relative_size = val_ratio / (train_ratio + val_ratio)

        train_idx, val_idx = train_test_split(
            train_val_idx,
            test_size=val_relative_size,
            stratify=train_val_labels,
            random_state=random_state
        )

        # Crear datasets
        splits = {
            'train': [all_samples[i] for i in train_idx],
            'val': [all_samples[i] for i in val_idx],
            'test': [all_samples[i] for i in test_idx]
        }

        # Estadísticas por split
        print("\nDistribución de splits:")
        for split_name, split_data in splits.items():
            print(f"  {split_name}: {len(split_data)} muestras")

        # Guardar splits
        splits_dir = self.data_dir / 'processed/splits'
        splits_dir.mkdir(parents=True, exist_ok=True)

        # Guardar cada split
        for split_name, split_data in splits.items():
            # Preparar arrays numpy
            X = np.array([s['features'] for s in split_data])
            y = np.array([s['label'] for s in split_data])
            landmarks = np.array([s['landmarks'] for s in split_data])

            split_file = splits_dir / f'{split_name}.pkl'
            with open(split_file, 'wb') as f:
                pickle.dump({
                    'X': X,
                    'y': y,
                    'landmarks': landmarks,
                    'samples': split_data
                }, f)

            print(f"  Guardado: {split_file}")

        # Guardar metadata
        metadata = {
            'label_to_idx': label_to_idx,
            'idx_to_label': {v: k for k, v in label_to_idx.items()},
            'num_classes': len(label_to_idx),
            'num_features': 63,  # 21 landmarks * 3 coords
            'splits': {
                'train': len(splits['train']),
                'val': len(splits['val']),
                'test': len(splits['test'])
            },
            'ratios': {
                'train': train_ratio,
                'val': val_ratio,
                'test': test_ratio
            }
        }

        metadata_file = splits_dir / 'metadata.json'
        with open(metadata_file, 'w') as f:
            json.dump(metadata, f, indent=2)

        print(f"  Guardado: {metadata_file}")

        # Mostrar distribución por clase en cada split
        print("\nDistribución por clase:")
        for split_name, split_data in splits.items():
            class_counts = defaultdict(int)
            for sample in split_data:
                class_counts[sample['gloss']] += 1

            min_c = min(class_counts.values())
            max_c = max(class_counts.values())
            print(f"  {split_name}: min={min_c}, max={max_c}")

        return splits, metadata

    def run(self):
        """Ejecuta creación de splits desde dataset Kaggle procesado"""
        # Paso 1: Verificar datos existentes
        clean_stats = self.skip_cleaning()

        # Paso 2: Crear splits
        splits, metadata = self.create_splits()

        print("\n" + "=" * 60)
        print("PROCESO COMPLETADO")
        print("=" * 60)
        print(f"Dataset limpio en: {self.output_dir}")
        print(f"Splits en: {self.data_dir / 'processed/splits'}")
        print(f"\nClases: {metadata['num_classes']}")
        print(f"Features: {metadata['num_features']}")
        print(f"Train: {metadata['splits']['train']}")
        print(f"Val: {metadata['splits']['val']}")
        print(f"Test: {metadata['splits']['test']}")

        return clean_stats, splits, metadata


def main():
    import argparse

    parser = argparse.ArgumentParser(description='Limpiar datos y crear dataset unificado')
    parser.add_argument('--data_dir', type=str,
                       default='/fs/nexus-scratch/bdepedro/TFM_ASL_VR/data',
                       help='Directorio de datos')

    args = parser.parse_args()

    cleaner = DatasetCleaner(args.data_dir)
    cleaner.run()


if __name__ == "__main__":
    main()
