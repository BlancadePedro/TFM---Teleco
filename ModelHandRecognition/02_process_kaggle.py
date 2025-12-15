#!/usr/bin/env python3
"""
Procesa completamente los datasets de Kaggle:
- ASL Alphabet (26 letras)
- ASL Digits (10 números)
Genera un dataset limpio y balanceado
"""

import os
import pickle
import numpy as np
from pathlib import Path
from tqdm import tqdm
import cv2
import mediapipe as mp

# Mapeos
DIGIT_TO_NAME = {
    '0': 'zero', '1': 'one', '2': 'two', '3': 'three', '4': 'four',
    '5': 'five', '6': 'six', '7': 'seven', '8': 'eight', '9': 'nine'
}

class KaggleProcessor:
    def __init__(self, data_dir: str, max_per_class: int = 50):
        self.data_dir = Path(data_dir)
        self.output_dir = self.data_dir / 'processed/kaggle_only'
        self.max_per_class = max_per_class

        # Paths a los datasets
        self.alphabet_dir = self.data_dir / 'raw/kaggle_asl_alphabet/asl_alphabet_train/asl_alphabet_train'
        self.digits_dir = self.data_dir / 'raw/kaggle_asl_digits/ASL Digits/asl_dataset_digits'

        # Inicializar MediaPipe
        self.mp_hands = mp.solutions.hands
        self.hands = self.mp_hands.Hands(
            static_image_mode=True,
            max_num_hands=1,
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5
        )

    def process_image(self, image_path: Path) -> dict:
        """Procesa una imagen y extrae landmarks"""
        image = cv2.imread(str(image_path))
        if image is None:
            return None

        image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        results = self.hands.process(image_rgb)

        if not results.multi_hand_landmarks:
            return None

        hand_landmarks = results.multi_hand_landmarks[0]
        landmarks = np.array([
            [lm.x, lm.y, lm.z]
            for lm in hand_landmarks.landmark
        ])

        return {
            'landmarks': landmarks.reshape(1, 21, 3),
            'confidence': results.multi_handedness[0].classification[0].score
        }

    def process_alphabet(self):
        """Procesa todas las letras del alfabeto"""
        print("\n" + "=" * 60)
        print("PROCESANDO ALFABETO (A-Z)")
        print("=" * 60)

        stats = {}

        for letter in 'ABCDEFGHIJKLMNOPQRSTUVWXYZ':
            letter_dir = self.alphabet_dir / letter
            if not letter_dir.exists():
                print(f"  {letter}: directorio no encontrado")
                continue

            # Crear directorio de salida
            gloss = letter.lower()
            output_class_dir = self.output_dir / gloss
            output_class_dir.mkdir(parents=True, exist_ok=True)

            # Limpiar archivos existentes
            for f in output_class_dir.glob('*.pkl'):
                f.unlink()

            # Obtener imágenes
            images = list(letter_dir.glob('*.jpg')) + list(letter_dir.glob('*.jpeg')) + list(letter_dir.glob('*.png'))
            images = images[:self.max_per_class * 2]  # Tomar más por si algunas fallan

            processed = 0
            for i, img_path in enumerate(tqdm(images, desc=f"  {letter}", leave=False)):
                if processed >= self.max_per_class:
                    break

                result = self.process_image(img_path)
                if result is None:
                    continue

                data = {
                    'landmarks': result['landmarks'],
                    'gloss': gloss,
                    'video_id': f"kaggle_alphabet_{gloss}_{processed:04d}",
                    'confidence': result['confidence'],
                    'source': 'kaggle_asl_alphabet',
                    'is_static': True
                }

                output_file = output_class_dir / f"kaggle_{gloss}_{processed:04d}.pkl"
                with open(output_file, 'wb') as f:
                    pickle.dump(data, f)

                processed += 1

            stats[gloss] = processed
            print(f"  {letter} -> {gloss}: {processed} muestras")

        return stats

    def process_digits(self):
        """Procesa todos los dígitos (0-9)"""
        print("\n" + "=" * 60)
        print("PROCESANDO DÍGITOS (0-9)")
        print("=" * 60)

        stats = {}

        for digit in '0123456789':
            digit_dir = self.digits_dir / digit
            if not digit_dir.exists():
                print(f"  {digit}: directorio no encontrado")
                continue

            # Crear directorio de salida
            gloss = DIGIT_TO_NAME[digit]
            output_class_dir = self.output_dir / gloss
            output_class_dir.mkdir(parents=True, exist_ok=True)

            # Limpiar archivos existentes
            for f in output_class_dir.glob('*.pkl'):
                f.unlink()

            # Obtener imágenes
            images = list(digit_dir.glob('*.jpg')) + list(digit_dir.glob('*.jpeg')) + list(digit_dir.glob('*.png'))
            images = images[:self.max_per_class * 2]

            processed = 0
            for i, img_path in enumerate(tqdm(images, desc=f"  {digit} ({gloss})", leave=False)):
                if processed >= self.max_per_class:
                    break

                result = self.process_image(img_path)
                if result is None:
                    continue

                data = {
                    'landmarks': result['landmarks'],
                    'gloss': gloss,
                    'video_id': f"kaggle_digits_{gloss}_{processed:04d}",
                    'confidence': result['confidence'],
                    'source': 'kaggle_asl_digits',
                    'is_static': True
                }

                output_file = output_class_dir / f"kaggle_{gloss}_{processed:04d}.pkl"
                with open(output_file, 'wb') as f:
                    pickle.dump(data, f)

                processed += 1

            stats[gloss] = processed
            print(f"  {digit} -> {gloss}: {processed} muestras")

        return stats

    def run(self):
        """Ejecuta el procesamiento completo"""
        print("=" * 60)
        print("PROCESAMIENTO COMPLETO DE KAGGLE")
        print("=" * 60)
        print(f"\nOutput: {self.output_dir}")
        print(f"Max por clase: {self.max_per_class}")

        # Crear directorio de salida
        self.output_dir.mkdir(parents=True, exist_ok=True)

        # Procesar alfabeto
        alphabet_stats = self.process_alphabet()

        # Procesar dígitos
        digits_stats = self.process_digits()

        # Resumen
        all_stats = {**alphabet_stats, **digits_stats}
        total = sum(all_stats.values())

        print("\n" + "=" * 60)
        print("RESUMEN")
        print("=" * 60)
        print(f"Total clases: {len(all_stats)}")
        print(f"Total muestras: {total}")
        print(f"Min por clase: {min(all_stats.values())}")
        print(f"Max por clase: {max(all_stats.values())}")
        print(f"Promedio: {total / len(all_stats):.1f}")

        return all_stats


def main():
    import argparse

    parser = argparse.ArgumentParser(description='Procesar datasets Kaggle completos')
    parser.add_argument('--data_dir', type=str,
                       default='/fs/nexus-scratch/bdepedro/TFM_ASL_VR/data',
                       help='Directorio de datos')
    parser.add_argument('--max_per_class', type=int, default=50,
                       help='Máximo de muestras por clase')

    args = parser.parse_args()

    processor = KaggleProcessor(
        data_dir=args.data_dir,
        max_per_class=args.max_per_class
    )
    processor.run()


if __name__ == "__main__":
    main()
