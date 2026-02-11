using UnityEngine;
using ASL_LearnVR.Feedback;

namespace ASL_LearnVR.LearningModule.GuideHand
{
    /// <summary>
    /// Biblioteca de poses ASL predefinidas para las manos guía.
    /// Convierte FingerConstraintProfile a HandPoseData con valores de rotación específicos.
    /// </summary>
    public static class ASLPoseLibrary
    {
        /// <summary>
        /// Convierte un FingerConstraintProfile a HandPoseData.
        /// Usa el punto medio de los rangos de curl como valor objetivo.
        /// </summary>
        public static HandPoseData FromConstraintProfile(FingerConstraintProfile profile)
        {
            if (profile == null)
                return HandPoseData.OpenHand();

            var pose = new HandPoseData
            {
                poseName = profile.signName
            };

            // Convertir thumb
            pose.thumb = ConvertThumbConstraint(profile.thumb);

            // Convertir dedos
            pose.index = ConvertFingerConstraint(profile.index);
            pose.middle = ConvertFingerConstraint(profile.middle);
            pose.ring = ConvertFingerConstraint(profile.ring);
            pose.pinky = ConvertFingerConstraint(profile.pinky);

            // Orientación de muñeca si está definida
            if (profile.checkOrientation)
            {
                pose.wristRotationOffset = CalculateWristRotation(profile.expectedPalmDirection);
            }

            return pose;
        }

        /// <summary>
        /// Obtiene una pose predefinida por nombre de signo.
        /// </summary>
        public static HandPoseData GetPoseBySignName(string signName)
        {
            Debug.Log($"[ASLPoseLibrary] GetPoseBySignName llamado con: '{signName}'");

            if (string.IsNullOrEmpty(signName))
            {
                Debug.LogWarning("[ASLPoseLibrary] signName vacío, retornando OpenHand");
                return HandPoseData.OpenHand();
            }

            // PRIMERO: Buscar en poses predefinidas (tienen prioridad porque están ajustadas manualmente)
            var predefinedPose = GetPredefinedPose(signName.ToUpper());
            if (predefinedPose != null)
            {
                Debug.Log($"[ASLPoseLibrary] Usando pose PREDEFINIDA para '{signName}'");
                return predefinedPose;
            }

            // SEGUNDO: Intentar obtener del perfil de constraints (para letras sin pose predefinida)
            var profile = AlphabetConstraintProfiles.GetLetterProfile(signName);
            if (profile != null)
            {
                Debug.Log($"[ASLPoseLibrary] Usando perfil de AlphabetConstraintProfiles para '{signName}'");
                return FromConstraintProfile(profile);
            }

            // TERCERO: Dígitos
            if (int.TryParse(signName, out int digit))
            {
                profile = DigitConstraintProfiles.GetDigitProfile(digit);
                if (profile != null)
                {
                    Debug.Log($"[ASLPoseLibrary] Usando perfil de DigitConstraintProfiles para '{digit}'");
                    return FromConstraintProfile(profile);
                }
            }

            Debug.LogWarning($"[ASLPoseLibrary] No se encontró pose para '{signName}', usando OpenHand");
            return HandPoseData.OpenHand();
        }

        /// <summary>
        /// Obtiene una pose predefinida si existe.
        /// </summary>
        private static HandPoseData GetPredefinedPose(string signName)
        {
            return signName switch
            {
                "A" => CreateLetterA(),
                "B" => CreateLetterB(),
                "C" => CreateLetterC(),
                "D" => CreateLetterD(),
                "E" => CreateLetterE(),
                "F" => CreateLetterF(),
                "G" => CreateLetterG(),
                "H" => CreateLetterH(),
                "I" => CreateLetterI(),
                "K" => CreateLetterK(),
                "L" => CreateLetterL(),
                "M" => CreateLetterM(),
                "N" => CreateLetterN(),
                "O" => CreateLetterO(),
                "P" => CreateLetterP(),
                "Q" => CreateLetterQ(),
                "R" => CreateLetterR(),
                "S" => CreateLetterS(),
                "T" => CreateLetterT(),
                "U" => CreateLetterU(),
                "V" => CreateLetterV(),
                "W" => CreateLetterW(),
                "X" => CreateLetterX(),
                "Y" => CreateLetterY(),
                "1" => CreateDigit1(),
                "2" => CreateDigit2(),
                "3" => CreateDigit3(),
                "4" => CreateDigit4(),
                "5" => CreateDigit5(),
                "6" => CreateDigit6(),
                "7" => CreateDigit7(),
                "8" => CreateDigit8(),
                "9" => CreateDigit9(),
                "0" => CreateDigit0(),
                _ => null  // No hay pose predefinida para esta letra
            };
        }

        private static ThumbPoseData ConvertThumbConstraint(ThumbConstraint constraint)
        {
            if (constraint == null || !constraint.curl.isEnabled)
                return ThumbPoseData.ExtendedBeside;

            float targetCurl = (constraint.curl.minCurl + constraint.curl.maxCurl) / 2f;

            var thumbPose = ThumbPoseData.FromCurlValue(targetCurl);

            // Ajustar según posición especial
            if (constraint.shouldBeOverFingers)
            {
                thumbPose = ThumbPoseData.OverFingers;
            }
            else if (constraint.shouldBeBesideFingers)
            {
                thumbPose = ThumbPoseData.ExtendedBeside;
            }
            else if (constraint.shouldTouchIndex || constraint.shouldTouchMiddle)
            {
                thumbPose = ThumbPoseData.TouchingFinger;
            }

            return thumbPose;
        }

        private static FingerPoseData ConvertFingerConstraint(FingerConstraint constraint)
        {
            if (constraint == null || !constraint.curl.isEnabled)
                return FingerPoseData.Extended;

            float targetCurl = (constraint.curl.minCurl + constraint.curl.maxCurl) / 2f;

            // Determinar tipo de curl basado en rangos
            if (targetCurl >= 0.85f)
            {
                return FingerPoseData.FullyCurled;
            }
            else if (targetCurl >= 0.55f && targetCurl < 0.85f)
            {
                // Tip curl (E, M, N)
                return FingerPoseData.TipCurl;
            }
            else if (targetCurl >= 0.3f)
            {
                return FingerPoseData.PartiallyCurled;
            }
            else
            {
                return FingerPoseData.Extended;
            }
        }

        private static Vector3 CalculateWristRotation(Vector3 expectedPalmDirection)
        {
            // Convertir dirección de palma a rotación de muñeca
            if (expectedPalmDirection == Vector3.right)
                return new Vector3(0, -90, 0); // Palma hacia la derecha
            if (expectedPalmDirection == Vector3.left)
                return new Vector3(0, 90, 0); // Palma hacia la izquierda
            if (expectedPalmDirection == Vector3.down)
                return new Vector3(-90, 0, 0); // Palma hacia abajo
            if (expectedPalmDirection == Vector3.up)
                return new Vector3(90, 0, 0); // Palma hacia arriba
            if (expectedPalmDirection == Vector3.forward)
                return Vector3.zero; // Palma hacia adelante (neutral)

            return Vector3.zero;
        }

        #region Predefined ASL Poses

        /// <summary>
        /// Letra A: Puño con pulgar al lado.
        /// </summary>
        public static HandPoseData CreateLetterA()
        {
            return new HandPoseData
            {
                poseName = "A",
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.15f,
                    proximalCurl = 0.2f,
                    distalCurl = 0.15f,
                    abductionAngle = 20f,
                    oppositionAngle = 0f
                },
                index = FingerPoseData.FullyCurled,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra B: Dedos extendidos y juntos (tocándose), pulgar cruzado en palma.
        /// </summary>
        public static HandPoseData CreateLetterB()
        {
            return new HandPoseData
            {
                poseName = "B",
                thumb = ThumbPoseData.AcrossPalm,
                // Dedos hacia el centro para que se junten y se toquen
                // INVERTIDO: positivo = hacia centro para índice, negativo = hacia centro para meñique
                index = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 5f  // Índice hacia el centro (hacia meñique)
                },
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 1f// Corazón ligeramente hacia el centro
                },
                ring = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = -1f // Anular ligeramente hacia el centro
                },
                pinky = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = -5f  // Meñique hacia el centro (hacia índice)
                }
            };
        }

        /// <summary>
        /// Letra C: Mano curvada en forma de C.
        /// Pulgar curvado hacia el índice pero sin llegar a tocar.
        /// </summary>
        public static HandPoseData CreateLetterC()
        {
            // Dedos curvados formando la parte superior de la C
            var cCurve = new FingerPoseData
            {
                metacarpalCurl = 0.1f,
                proximalCurl = 0.35f,
                intermediateCurl = 0.4f,
                distalCurl = 0.35f,
                spreadAngle = 0f
            };

            return new HandPoseData
            {
                poseName = "C",
                // Rotación para ver la C desde el lado
                wristRotationOffset = new Vector3(0f, 0f, 90f),
                // Pulgar curvado formando la parte INFERIOR de la C
                // Debe estar bien separado del índice para formar la apertura de la C
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.35f,
                    proximalCurl = 0.4f,
                    distalCurl = 0.2f,
                    abductionAngle = 25f,
                    oppositionAngle = 15f,
                    distalTwist = -50f,    // Rotar uña hacia arriba
                    thumbPitch =-15f       // Hacia el usuario
                },
                index = cCurve,
                middle = cCurve,
                ring = cCurve,
                pinky = cCurve
            };
        }

        /// <summary>
        /// Letra D: Índice extendido, otros tocan pulgar.
        /// </summary>
        public static HandPoseData CreateLetterD()
        {
            return new HandPoseData
            {
                poseName = "D",
                thumb = ThumbPoseData.TouchingFinger,
                index = FingerPoseData.Extended,
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0.15f,
                    proximalCurl = 0.6f,
                    intermediateCurl = 0.7f,
                    distalCurl = 0.5f,
                    spreadAngle = 0f
                },
                ring = new FingerPoseData
                {
                    metacarpalCurl = 0.15f,
                    proximalCurl = 0.6f,
                    intermediateCurl = 0.7f,
                    distalCurl = 0.5f,
                    spreadAngle = 0f
                },
                pinky = new FingerPoseData
                {
                    metacarpalCurl = 0.15f,
                    proximalCurl = 0.6f,
                    intermediateCurl = 0.7f,
                    distalCurl = 0.5f,
                    spreadAngle = 0f
                }
            };
        }

        /// <summary>
        /// Letra E: Puntas de dedos curvadas hacia palma (tip curl).
        /// </summary>
        public static HandPoseData CreateLetterE()
        {
            return new HandPoseData
            {
                poseName = "E",
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.3f,
                    proximalCurl = 0.5f,
                    distalCurl = 0.4f,
                    abductionAngle = -15f,
                    oppositionAngle = 15f
                },
                index = FingerPoseData.TipCurl,
                middle = FingerPoseData.TipCurl,
                ring = FingerPoseData.TipCurl,
                pinky = FingerPoseData.TipCurl
            };
        }

        /// <summary>
        /// Letra F: Índice y pulgar forman círculo (OK sign), otros extendidos.
        /// </summary>
        public static HandPoseData CreateLetterF()
        {
            return new HandPoseData
            {
                poseName = "F",
                // Pulgar curvado tocando el índice
                wristRotationOffset = new Vector3(0f, 0f, 160f),
                // RANGOS: abductionAngle [-45,45], oppositionAngle [-30,30]
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.35f,
                    proximalCurl = 0.4f,
                    distalCurl = 0.2f,
                    abductionAngle = 25f,
                    oppositionAngle = 15f,
                    distalTwist = -50f,    // Rotar uña hacia arriba
                    thumbPitch =-15f       // Hacia el usuario
                },
                // Índice curvado para tocar el pulgar
                index = new FingerPoseData
                {
                    metacarpalCurl = 0.15f,
                    proximalCurl = 0.55f,
                    intermediateCurl = 0.6f,
                    distalCurl = 0.5f,
                    spreadAngle = 0f
                },
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.Extended,
                pinky = FingerPoseData.Extended
            };
        }

        /// <summary>
        /// Letra G: Pulgar e índice extendidos en paralelo, apuntando al lado.
        /// </summary>
        public static HandPoseData CreateLetterG()
        {
            return new HandPoseData
            {
                poseName = "G",
                // Dedos apuntando a la izquierda (-90 Z) + girada sobre muñeca (-90 X) para ver pulgar-índice paralelos
                wristRotationOffset = new Vector3(0f, 90f, 90f),
                wristPositionOffset = new Vector3(-0.08f, 0f, 0.12f), // Subir para no meter en la mesa

                // Pulgar extendido en paralelo al índice
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle = 0f,       // Paralelo al índice (no separado)
                    oppositionAngle = 0f,
                    distalTwist = 0f,
                    thumbPitch = 0f
                },

                // Índice completamente extendido
                index = FingerPoseData.Extended,

                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra H: Índice y corazón extendidos y juntos, horizontales.
        /// </summary>
        public static HandPoseData CreateLetterH()
        {
            return new HandPoseData
            {
                poseName = "H",
                // Dedos apuntando a la izquierda, palma al usuario
                wristRotationOffset = new Vector3(0f, 90f, 0f),
                wristPositionOffset = new Vector3(-0.08f, 0f, 0.1f),
                thumb = ThumbPoseData.AcrossPalm,

                // Índice y corazón MUY juntos
                index = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 1f   // Más hacia el corazón
                },
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = -3f  // Más hacia el índice
                },

                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra I: Solo meñique extendido.
        /// </summary>
        public static HandPoseData CreateLetterI()
        {
            return new HandPoseData
            {
                poseName = "I",
                thumb = ThumbPoseData.AcrossPalm,
                index = FingerPoseData.FullyCurled,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.Extended
            };
        }

        /// <summary>
        /// Letra K: Índice y corazón en V, pulgar recto paralelo al índice tocándolo.
        /// </summary>
        public static HandPoseData CreateLetterK()
        {
            return new HandPoseData
            {
                poseName = "K",
                wristRotationOffset = new Vector3(0f, 0f, 30f),

                // Pulgar recto, paralelo al índice, pegado a él
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle = 20f,          // Más hacia el índice
                    oppositionAngle = 25f,         // Cruzar más hacia el índice
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.Extended,
                // Corazón extendido y separado del índice (forma V)
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 5f  // Separado del índice
                },
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra L: Pulgar e índice extendidos en forma de L.
        /// Pulgar más horizontal para formar una L más marcada.
        /// </summary>
        public static HandPoseData CreateLetterL()
        {
            return new HandPoseData
            {
                poseName = "L",
                wristRotationOffset = new Vector3(0f, 0f,-15f),
                // Pulgar extendido y perpendicular al índice
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle =-60f,
                    oppositionAngle = -10f,
                    distalTwist = 50f, 
                    thumbPitch = 0f
                },
                index = FingerPoseData.Extended,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra M: Índice, corazón y anular en tip curl, meñique full curl,
        /// pulgar lo más cruzado posible (metido debajo de los dedos).
        /// </summary>
        public static HandPoseData CreateLetterM()
        {
            return new HandPoseData
            {
                poseName = "M",
                // Pulgar cruzado pero más estirado
             thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.4f,
                    proximalCurl = 0.6f,
                    distalCurl = 0.5f,
                    abductionAngle = -20f,     // Hacia dentro
                    oppositionAngle = 25f,     // Cruzado hacia dedos
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.TipCurl,
                middle = FingerPoseData.TipCurl,
                ring = FingerPoseData.TipCurl,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra N: Índice y corazón en tip curl, anular y meñique full curl,
        /// pulgar cruzado en mitad de la palma.
        /// </summary>
        public static HandPoseData CreateLetterN()
        {
            return new HandPoseData
            {
                poseName = "N",
                // Pulgar cruzado en mitad de palma
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.4f,
                    proximalCurl = 0.6f,
                    distalCurl = 0.5f,
                    abductionAngle = -20f,     // Hacia dentro
                    oppositionAngle = 25f,     // Cruzado hacia dedos
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.TipCurl,
                middle = FingerPoseData.TipCurl,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra O: Pulgar tocando índice formando un círculo.
        /// </summary>
        public static HandPoseData CreateLetterO()
        {
            // Dedos curvados acompañando la O
            var partiallyCurled = new FingerPoseData
            {
                metacarpalCurl = 0.15f,
                proximalCurl = 0.5f,
                intermediateCurl = 0.55f,
                distalCurl = 0.5f,
                spreadAngle = 0f
            };

            return new HandPoseData
            {
                poseName = "O",
                // Pulgar curvado tocando el índice - forma círculo
                wristRotationOffset = new Vector3(0f, 0f, 90f),
                // RANGOS: abductionAngle [-45,45], oppositionAngle [-30,30]
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.35f,
                    proximalCurl = 0.4f,
                    distalCurl = 0.2f,
                    abductionAngle = 25f,
                    oppositionAngle = 15f,
                    distalTwist = -50f,    // Rotar uña hacia arriba
                    thumbPitch =-15f       // Hacia el usuario
                },
                // Índice más curvado para tocar el pulgar
                index = new FingerPoseData
                {
                    metacarpalCurl = 0.15f,
                    proximalCurl = 0.55f,
                    intermediateCurl = 0.6f,
                    distalCurl = 0.5f,
                    spreadAngle = 0f
                },
                middle = partiallyCurled,
                ring = partiallyCurled,
                pinky = partiallyCurled
            };
        }

        /// <summary>
        /// Letra P: Igual que K (pulgar e índice paralelos, corazón en V) pero apuntando hacia abajo.
        /// </summary>
        public static HandPoseData CreateLetterP()
        {
            return new HandPoseData
            {
                poseName = "P",
                // Como K pero apuntando abajo
                wristRotationOffset = new Vector3(0f, 180f, 30f),
                wristPositionOffset = new Vector3(0f, 0f, 0.2f),
                // Pulgar recto, paralelo al índice, pegado a él
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle = 20f,          // Más hacia el índice
                    oppositionAngle = 25f,         // Cruzar más hacia el índice
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.Extended,
                // Corazón extendido y separado del índice (forma V)
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 5f  // Separado del índice
                },
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra Q: Igual que G (pulgar e índice paralelos) pero apuntando hacia abajo.
        /// </summary>
        public static HandPoseData CreateLetterQ()
        {
            return new HandPoseData
            {
                poseName = "Q",
                // Mano girada para que índice apunte hacia abajo
                wristRotationOffset = new Vector3(0f, 180f, 90f),
                wristPositionOffset = new Vector3(0f, 0f, 0.2f),

                // Pulgar extendido en paralelo al índice
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle = 0f,       // Paralelo al índice
                    oppositionAngle = 0f,
                    distalTwist = 0f,
                    thumbPitch = 0f
                },

                // Índice completamente extendido
                index = FingerPoseData.Extended,

                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra R: Índice y corazón cruzados (corazón sobre índice).
        /// </summary>
        public static HandPoseData CreateLetterR()
        {
            return new HandPoseData
            {
                poseName = "R",
                thumb = ThumbPoseData.AcrossPalm,

                // Índice extendido, ligeramente separado para dejar cruzar el corazón
                index = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 10f  // Hacia fuera para que el corazón pase por encima
                },
                // Corazón cruza sobre el índice
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = -10f   // Cruza por encima del índice
                },

                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra S: Puño con pulgar sobre los dedos.
        /// </summary>
        public static HandPoseData CreateLetterS()
        {
            return new HandPoseData
            {
                poseName = "S",
                thumb = ThumbPoseData.OverFingers,
                index = FingerPoseData.FullyCurled,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
                }

        /// <summary>
        /// Letra T: Índice en tip curl, resto full curl,
        /// pulgar cerca del índice (metido entre índice y corazón).
        /// </summary>
        public static HandPoseData CreateLetterT()
        {
            return new HandPoseData
            {
                poseName = "T",
                // Pulgar cerca del índice, metido entre índice y corazón
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.3f,
                    proximalCurl = 0.5f,
                    distalCurl = 0.4f,
                    abductionAngle = 10f,      // Hacia el índice
                    oppositionAngle = 20f,     // Cruzar hacia el índice
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.TipCurl,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra U: Índice y corazón extendidos juntos, resto cerrados.
        /// </summary>
        public static HandPoseData CreateLetterU()
        {
            return new HandPoseData
            {
                poseName = "U",
                thumb = ThumbPoseData.AcrossPalm,

                // Índice y corazón MUY juntos (como H pero sin girar muñeca)
                index = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 0f   // Hacia el corazón
                },
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = -3f  // Hacia el índice
                },

                // Resto cerrados
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }


        /// <summary>
        /// Letra V: Índice y medio extendidos en V.
        /// </summary>
        public static HandPoseData CreateLetterV()
        {
            return new HandPoseData
            {
                poseName = "V",
                thumb = ThumbPoseData.AcrossPalm,
                index = FingerPoseData.Extended,
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra W: Índice, medio y anular extendidos.
        /// </summary>
        public static HandPoseData CreateLetterW()
        {
            return new HandPoseData
            {
                poseName = "W",
                // Pulgar cruzado sobre la palma sujetando el meñique
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.4f,
                    proximalCurl = 0.6f,
                    distalCurl = 0.5f,
                    abductionAngle = -25f,     // Más hacia dentro
                    oppositionAngle = 30f      // Cruzar hacia meñique
                },
                index = FingerPoseData.Extended,
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.Extended,
                // Meñique bien curvado hacia el pulgar, más diagonal
                pinky = new FingerPoseData
                {
                    metacarpalCurl = 0.3f,
                    proximalCurl = 0.6f,       // Muy curvado
                    intermediateCurl = 0.65f,  // Muy curvado
                    distalCurl = 0.4f,         // Muy curvado
                    spreadAngle = 0f         // Mucho más hacia el pulgar (diagonal)
                }
            };
        }

        /// <summary>
        /// Letra X: Índice en forma de gancho, resto cerrados.
        /// </summary>
        public static HandPoseData CreateLetterX()
        {
            return new HandPoseData
            {
                poseName = "X",
                thumb = ThumbPoseData.AcrossPalm,

                // Índice en forma de GANCHO (hook)
                wristRotationOffset = new Vector3(0f, 0f, 60f),
                // Proximal ligeramente curvado, intermediate y distal muy curvados
                index = new FingerPoseData
                {
                    metacarpalCurl = 0.1f,
                    proximalCurl = 0.2f,      // Ligeramente curvado
                    intermediateCurl = 0.85f, // Muy curvado
                    distalCurl = 0.9f,        // Muy curvado (punta del gancho)
                    spreadAngle = 0f
                },

                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Letra Y: Pulgar y meñique extendidos (hang loose).
        /// </summary>
        public static HandPoseData CreateLetterY()
        {
            return new HandPoseData
            {
                poseName = "Y",
                 thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle =-60f,
                    oppositionAngle = -10f,
                    distalTwist = 50f, 
                    thumbPitch = 0f
                },
                index = FingerPoseData.FullyCurled,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 20f
                }
            };
        }

        /// <summary>
        /// Dígito 1: Índice extendido.
        /// </summary>
        public static HandPoseData CreateDigit1()
        {
            return new HandPoseData
            {
                poseName = "1",
                thumb = ThumbPoseData.AcrossPalm,
                index = FingerPoseData.Extended,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Dígito 2: Índice y medio extendidos.
        /// </summary>
        public static HandPoseData CreateDigit2()
        {
            return new HandPoseData
            {
                poseName = "2",
                thumb = ThumbPoseData.AcrossPalm,
                index = FingerPoseData.Extended,
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Dígito 3: Pulgar, índice y medio extendidos.
        /// </summary>
        public static HandPoseData CreateDigit3()
        {
            return new HandPoseData
            {
                poseName = "3",
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle =-60f,
                    oppositionAngle = -10f,
                    distalTwist = 50f, 
                    thumbPitch = 0f
                },
                index = FingerPoseData.Extended,
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Dígito 4: Cuatro dedos extendidos, pulgar cerrado.
        /// </summary>
        public static HandPoseData CreateDigit4()
        {
            return new HandPoseData
            {
                poseName = "4",
                thumb = ThumbPoseData.AcrossPalm,
                index = FingerPoseData.Extended,
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.Extended,
                pinky = FingerPoseData.Extended
            };
        }

        /// <summary>
        /// Dígito 5: Mano abierta, todos los dedos extendidos.
        /// </summary>
        public static HandPoseData CreateDigit5()
        {
            // Mano abierta con dedos separados entre sí
            return new HandPoseData
            {
                poseName = "5",
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle =-60f,
                    oppositionAngle = -10f,
                    distalTwist = 50f,
                    thumbPitch = 0f
                },
                index = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = -5f  // Separar del medio (hacia el pulgar)
                },
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = -2f  // Ligeramente hacia el índice
                },
                ring = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 2f   // Ligeramente hacia el meñique
                },
                pinky = new FingerPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    intermediateCurl = 0f,
                    distalCurl = 0f,
                    spreadAngle = 5f   // Separar del anular (hacia fuera)
                }
            };
        }

        /// <summary>
        /// Dígito 6: Pulgar toca meñique (tip curl), resto extendidos.
        /// </summary>
        public static HandPoseData CreateDigit6()
        {
            return new HandPoseData
            {
                poseName = "6",
                // Pulgar cruzado hacia meñique
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.4f,
                    proximalCurl = 0.6f,
                    distalCurl = 0.5f,
                    abductionAngle = -25f,     // Hacia dentro (meñique)
                    oppositionAngle = 30f,     // Máximo cruce hacia meñique
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.Extended,
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.Extended,
                // Meñique en tip curl para tocar el pulgar
                pinky = FingerPoseData.TipCurl
            };
        }

        /// <summary>
        /// Dígito 7: Pulgar toca anular (tip curl), índice+corazón+meñique extendidos.
        /// </summary>
        public static HandPoseData CreateDigit7()
        {
            return new HandPoseData
            {
                poseName = "7",
                // Pulgar cruzado hacia anular
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.35f,
                    proximalCurl = 0.55f,
                    distalCurl = 0.45f,
                    abductionAngle = -15f,     // Hacia dentro (anular)
                    oppositionAngle = 25f,     // Cruce hacia anular
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.Extended,
                middle = FingerPoseData.Extended,
                // Anular en tip curl para tocar el pulgar
                ring = FingerPoseData.TipCurl,
                pinky = FingerPoseData.Extended
            };
        }

        /// <summary>
        /// Dígito 8: Pulgar toca corazón (tip curl), índice+anular+meñique extendidos.
        /// </summary>
        public static HandPoseData CreateDigit8()
        {
            return new HandPoseData
            {
                poseName = "8",
                // Pulgar cruzado hacia corazón
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.3f,
                    proximalCurl = 0.45f,
                    distalCurl = 0.4f,
                    abductionAngle = -5f,      // Ligeramente hacia dentro
                    oppositionAngle = 20f,     // Cruce hacia corazón
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.Extended,
                // Corazón en tip curl para tocar el pulgar
                middle = FingerPoseData.TipCurl,
                ring = FingerPoseData.Extended,
                pinky = FingerPoseData.Extended
            };
        }

        /// <summary>
        /// Dígito 9: Pulgar toca índice (tip curl), corazón+anular+meñique extendidos.
        /// </summary>
        public static HandPoseData CreateDigit9()
        {
            return new HandPoseData
            {
                poseName = "9",
                // Pulgar hacia índice (similar a F/O)
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.3f,
                    proximalCurl = 0.4f,
                    distalCurl = 0.35f,
                    abductionAngle = 10f,      // Hacia el índice
                    oppositionAngle = 15f,     // Cruce hacia índice
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                // Índice en tip curl para tocar el pulgar
                index = FingerPoseData.TipCurl,
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.Extended,
                pinky = FingerPoseData.Extended
            };
        }

        /// <summary>
        /// Dígito 0: Igual que la letra O (pulgar toca índice formando círculo).
        /// </summary>
        public static HandPoseData CreateDigit0()
        {
            var partiallyCurled = new FingerPoseData
            {
                metacarpalCurl = 0.15f,
                proximalCurl = 0.5f,
                intermediateCurl = 0.55f,
                distalCurl = 0.5f,
                spreadAngle = 0f
            };

            return new HandPoseData
            {
                poseName = "0",
                wristRotationOffset = new Vector3(0f, 0f, 90f),
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.35f,
                    proximalCurl = 0.4f,
                    distalCurl = 0.2f,
                    abductionAngle = 25f,
                    oppositionAngle = 15f,
                    distalTwist = -50f,
                    thumbPitch = -15f
                },
                index = new FingerPoseData
                {
                    metacarpalCurl = 0.15f,
                    proximalCurl = 0.55f,
                    intermediateCurl = 0.6f,
                    distalCurl = 0.5f,
                    spreadAngle = 0f
                },
                middle = partiallyCurled,
                ring = partiallyCurled,
                pinky = partiallyCurled
            };
        }

        #endregion

        #region Animated Poses (J, Z, Basic Communication)

        /// <summary>
        /// Obtiene una secuencia animada si el signo la requiere.
        /// Retorna null si el signo no necesita animación.
        /// </summary>
        public static AnimatedPoseSequence GetAnimatedPose(string signName)
        {
            if (string.IsNullOrEmpty(signName))
                return null;

            return signName.ToUpper() switch
            {
                "J" => CreateLetterJ(),
                "Z" => CreateLetterZ(),
                "HELLO" => CreateSignHello(),
                "BYE" => CreateSignBye(),
                "YES" => CreateSignYes(),
                "NO" => CreateSignNo(),
                "THANK YOU" => CreateSignThankYou(),
                "PLEASE" => CreateSignPlease(),
                "GOOD" => CreateSignGood(),
                "BAD" => CreateSignBad(),
                _ => null
            };
        }

        /// <summary>
        /// Indica si un signo requiere animación de ambas manos.
        /// </summary>
        public static bool IsDoubleHandedSign(string signName)
        {
            if (string.IsNullOrEmpty(signName)) return false;
            return signName.ToUpper() switch
            {
                "THANK YOU" => true,
                _ => false
            };
        }

        /// <summary>
        /// Obtiene la secuencia animada para la mano IZQUIERDA si el signo es de dos manos.
        /// Retorna null si el signo no necesita mano izquierda.
        /// </summary>
        public static AnimatedPoseSequence GetAnimatedPoseLeftHand(string signName)
        {
            if (string.IsNullOrEmpty(signName)) return null;
            return signName.ToUpper() switch
            {
                "THANK YOU" => CreateSignThankYouLeftHand(), // Animación espejada (Z invertido)
                _ => null
            };
        }

        /// <summary>
        /// Letra J: Pose de I (meñique extendido) + muñeca dibuja una J.
        /// Movimiento: abajo + curva hacia la izquierda.
        /// </summary>
        private static AnimatedPoseSequence CreateLetterJ()
        {
            // Pose base: igual que I (meñique extendido)
            var basePose = CreateLetterI();

            return new AnimatedPoseSequence
            {
                poseName = "J",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    // Ejes locales: X=izquierda/derecha, Y=profundidad (NO TOCAR), Z=arriba/abajo (+ = arriba)
                    // Inicio: pose I bien arriba para no meter en la mesa
                    new PoseKeyframe(0f, new HandPoseData
                    {
                        poseName = "J_start",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(0f, 0f, 0.12f)
                    }),
                    // Bajar recto (Z negativo = abajo)
                    new PoseKeyframe(0.15f, new HandPoseData
                    {
                        poseName = "J_down",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(0f, 0f, 0f)
                    }),
                    // Curva hacia la izquierda (X negativo = izquierda)
                    new PoseKeyframe(0.3f, new HandPoseData
                    {
                        poseName = "J_curve",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(-0.06f, 0f, 0.02f)
                    }),
                    // Final curva: más a la izquierda, subiendo un poco
                    new PoseKeyframe(0.5f, new HandPoseData
                    {
                        poseName = "J_end",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(-0.08f, 0f, 0.06f)
                    }),
                    // Mantener pose final
                    new PoseKeyframe(1.0f, new HandPoseData
                    {
                        poseName = "J_hold",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(-0.08f, 0f, 0.06f)
                    })
                }
            };
        }

        /// <summary>
        /// Letra Z: Pose de 1 (índice extendido) + muñeca dibuja una Z.
        /// Movimiento: derecha + diagonal abajo-izquierda + derecha.
        /// </summary>
        private static AnimatedPoseSequence CreateLetterZ()
        {
            // Pose base: igual que 1 (índice extendido)
            var basePose = CreateDigit1();

            return new AnimatedPoseSequence
            {
                poseName = "Z",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    // Ejes locales: X=izquierda/derecha, Y=profundidad (NO TOCAR), Z=arriba/abajo (+ = arriba)
                    // Inicio: arriba-derecha (invertido: izq→der primero)
                    new PoseKeyframe(0f, new HandPoseData
                    {
                        poseName = "Z_start",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(0.04f, 0f, 0.08f)
                    }),
                    // Trazo horizontal: arriba-izquierda (primera línea de la Z: der→izq)
                    new PoseKeyframe(0.5f, new HandPoseData
                    {
                        poseName = "Z_left",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(-0.04f, 0f, 0.08f)
                    }),
                    // Diagonal: abajo-derecha (en el mismo plano X-Z, diagonal más corta)
                    new PoseKeyframe(1.0f, new HandPoseData
                    {
                        poseName = "Z_diag",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(0.04f, 0f, 0.02f)
                    }),
                    // Trazo horizontal: abajo-izquierda (última línea de la Z: der→izq)
                    new PoseKeyframe(1.5f, new HandPoseData
                    {
                        poseName = "Z_end",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(-0.04f, 0f, 0.02f)
                    }),
                    // Mantener pose final
                    new PoseKeyframe(2.0f, new HandPoseData
                    {
                        poseName = "Z_hold",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(-0.04f, 0f, 0.02f)
                    })
                }
            };
        }

        #region Basic Communication Signs

        /// <summary>
        /// Pose base de pulgar arriba (para "Good").
        /// Rotación Y+90 para que el pulgar apunte hacia arriba.
        /// </summary>
        private static HandPoseData CreateThumbsUpPose(Vector3 wristPos)
        {
            return new HandPoseData
            {
                poseName = "ThumbsUp",
                wristRotationOffset = new Vector3(0f, 90f, 0f), // Y+90: pulgar apunta arriba
                wristPositionOffset = wristPos,
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle = 30f,
                    oppositionAngle = 0f,
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.FullyCurled,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Pose base de pulgar abajo (para "Bad").
        /// Rotación Y-90 para que el pulgar apunte hacia abajo.
        /// </summary>
        private static HandPoseData CreateThumbsDownPose(Vector3 wristPos)
        {
            return new HandPoseData
            {
                poseName = "ThumbsDown",
                wristRotationOffset = new Vector3(0f, -90f, 0f), // Y-90: pulgar apunta abajo
                wristPositionOffset = wristPos,
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle = 30f,
                    oppositionAngle = 0f,
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = FingerPoseData.FullyCurled,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Pose de mano para Hello/ThankYou: dedos juntos (como B), pulgar extendido pegado al índice.
        /// </summary>
        private static HandPoseData CreateHelloHandPose(string name, Vector3 wristPos, Vector3 wristRot)
        {
            return new HandPoseData
            {
                poseName = name,
                wristRotationOffset = wristRot,
                wristPositionOffset = wristPos,
                // Pulgar extendido pero pegado al índice
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.1f,
                    proximalCurl = 0.15f,
                    distalCurl = 0.1f,
                    abductionAngle = 15f,   // Cerca del índice
                    oppositionAngle = 0f,
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                // Dedos juntos como B
                index = new FingerPoseData
                {
                    metacarpalCurl = 0f, proximalCurl = 0f, intermediateCurl = 0f, distalCurl = 0f,
                    spreadAngle = 3f   // Ligeramente hacia el centro
                },
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0f, proximalCurl = 0f, intermediateCurl = 0f, distalCurl = 0f,
                    spreadAngle = 1f
                },
                ring = new FingerPoseData
                {
                    metacarpalCurl = 0f, proximalCurl = 0f, intermediateCurl = 0f, distalCurl = 0f,
                    spreadAngle = -1f
                },
                pinky = new FingerPoseData
                {
                    metacarpalCurl = 0f, proximalCurl = 0f, intermediateCurl = 0f, distalCurl = 0f,
                    spreadAngle = -3f  // Ligeramente hacia el centro
                }
            };
        }

        /// <summary>
        /// Pose de mano para Please: dedos juntos y estirados (como B), pulgar cruzado.
        /// </summary>
        private static HandPoseData CreatePleaseHandPose(string name, Vector3 wristPos)
        {
            return new HandPoseData
            {
                poseName = name,
                wristRotationOffset = Vector3.zero,
                wristPositionOffset = wristPos,
                // Pulgar extendido pegado al índice (no doblado)
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0.1f,
                    proximalCurl = 0.15f,
                    distalCurl = 0.1f,
                    abductionAngle = 15f,
                    oppositionAngle = 0f,
                    distalTwist = 0f,
                    thumbPitch = 0f
                },
                index = new FingerPoseData
                {
                    metacarpalCurl = 0f, proximalCurl = 0f, intermediateCurl = 0f, distalCurl = 0f,
                    spreadAngle = 3f
                },
                middle = new FingerPoseData
                {
                    metacarpalCurl = 0f, proximalCurl = 0f, intermediateCurl = 0f, distalCurl = 0f,
                    spreadAngle = 1f
                },
                ring = new FingerPoseData
                {
                    metacarpalCurl = 0f, proximalCurl = 0f, intermediateCurl = 0f, distalCurl = 0f,
                    spreadAngle = -1f
                },
                pinky = new FingerPoseData
                {
                    metacarpalCurl = 0f, proximalCurl = 0f, intermediateCurl = 0f, distalCurl = 0f,
                    spreadAngle = -3f
                }
            };
        }

        /// <summary>
        /// Hello: Palma abierta con ola desde la muñeca (rotación Z oscilante).
        /// Dedos juntos, pulgar pegado al índice.
        /// </summary>
        private static AnimatedPoseSequence CreateSignHello()
        {
            return new AnimatedPoseSequence
            {
                poseName = "Hello",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    // Ola: rotación Z oscila (muñeca como eje)
                    new PoseKeyframe(0f, CreateHelloHandPose("Hello_center", new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, 0f))),
                    new PoseKeyframe(0.15f, CreateHelloHandPose("Hello_right", new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, -20f))),
                    new PoseKeyframe(0.35f, CreateHelloHandPose("Hello_left", new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, 20f))),
                    new PoseKeyframe(0.55f, CreateHelloHandPose("Hello_right2", new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, -20f))),
                    new PoseKeyframe(0.75f, CreateHelloHandPose("Hello_center2", new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, 0f))),
                    new PoseKeyframe(1.5f, CreateHelloHandPose("Hello_hold", new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, 0f)))
                }
            };
        }

        /// <summary>
        /// Bye: Palma abierta que se cierra (como A) repetidamente.
        /// </summary>
        private static AnimatedPoseSequence CreateSignBye()
        {
            return new AnimatedPoseSequence
            {
                poseName = "Bye",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    // Alternar entre palma abierta y puño A
                    new PoseKeyframe(0f, CreateHelloHandPose("Bye_open1", new Vector3(0f, 0f, 0.1f), Vector3.zero)),
                    new PoseKeyframe(0.3f, WithWristOffset(CreateLetterA(), "Bye_close1", new Vector3(0f, 0f, 0.1f))),
                    new PoseKeyframe(0.6f, CreateHelloHandPose("Bye_open2", new Vector3(0f, 0f, 0.1f), Vector3.zero)),
                    new PoseKeyframe(0.9f, WithWristOffset(CreateLetterA(), "Bye_close2", new Vector3(0f, 0f, 0.1f))),
                    new PoseKeyframe(1.2f, CreateHelloHandPose("Bye_open3", new Vector3(0f, 0f, 0.1f), Vector3.zero)),
                    new PoseKeyframe(1.8f, CreateHelloHandPose("Bye_hold", new Vector3(0f, 0f, 0.1f), Vector3.zero))
                }
            };
        }

        /// <summary>
        /// Yes: Puño (S) con movimiento de llamar a la puerta (Y oscila: adelante-atrás).
        /// </summary>
        private static AnimatedPoseSequence CreateSignYes()
        {
            var fist = CreateLetterS();

            return new AnimatedPoseSequence
            {
                poseName = "Yes",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    // Knocking: oscilación en Y (profundidad) - como tocar una puerta
                    new PoseKeyframe(0f, WithWristOffset(fist, "Yes_back", new Vector3(0.04f, 0f, 0.12f))),
                    new PoseKeyframe(0.12f, WithWristOffset(fist, "Yes_knock1", new Vector3(0.04f, 0.05f, 0.12f))),
                    new PoseKeyframe(0.22f, WithWristOffset(fist, "Yes_back2", new Vector3(0.04f, 0f, 0.12f))),
                    new PoseKeyframe(0.34f, WithWristOffset(fist, "Yes_knock2", new Vector3(0.04f, 0.05f, 0.12f))),
                    new PoseKeyframe(0.44f, WithWristOffset(fist, "Yes_back3", new Vector3(0.04f, 0f, 0.12f))),
                    new PoseKeyframe(1.0f, WithWristOffset(fist, "Yes_hold", new Vector3(0.04f, 0f, 0.12f)))
                }
            };
        }

        /// <summary>
        /// No: Pose H (índice y corazón horizontales) oscilando lado a lado.
        /// </summary>
        private static AnimatedPoseSequence CreateSignNo()
        {
            var hPose = CreateLetterH();

            return new AnimatedPoseSequence
            {
                poseName = "No",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    // H ya tiene wristRotationOffset=(0,90,0) → dedos horizontales
                    // Más arriba y a la derecha para no superponerse con la otra mano
                    new PoseKeyframe(0f, WithWristOffset(hPose, "No_center", new Vector3(0f, 0f, 0.16f))),
                    new PoseKeyframe(0.12f, WithWristOffset(hPose, "No_right", new Vector3(0.04f, 0f, 0.16f))),
                    new PoseKeyframe(0.3f, WithWristOffset(hPose, "No_left", new Vector3(-0.04f, 0f, 0.16f))),
                    new PoseKeyframe(0.48f, WithWristOffset(hPose, "No_right2", new Vector3(0.04f, 0f, 0.16f))),
                    new PoseKeyframe(0.6f, WithWristOffset(hPose, "No_center2", new Vector3(0f, 0f, 0.16f))),
                    new PoseKeyframe(1.2f, WithWristOffset(hPose, "No_hold", new Vector3(0f, 0f, 0.16f)))
                }
            };
        }

        /// <summary>
        /// Thank You (mano derecha): Empieza en posición default, manos ladeadas formando pico,
        /// luego se abren hacia adelante y horizontal.
        /// </summary>
        private static AnimatedPoseSequence CreateSignThankYou()
        {
            return new AnimatedPoseSequence
            {
                poseName = "ThankYou",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    // Inicio: posición default (sin offset), mano ladeada con dedos hacia el centro (pico)
                    new PoseKeyframe(0f, CreateHelloHandPose("TY_start", Vector3.zero, new Vector3(0f, 0f, 30f))),
                    // Intermedio: avanza, se va aplanando
                    new PoseKeyframe(0.3f, CreateHelloHandPose("TY_mid", new Vector3(0f, 0.06f, 0f), new Vector3(-30f, 0f, 20f))),
                    // Final: prácticamente horizontal, alejada
                    new PoseKeyframe(0.7f, CreateHelloHandPose("TY_end", new Vector3(0f, 0.14f, -0.05f), new Vector3(-70f, 0f, 10f))),
                    // Mantener
                    new PoseKeyframe(1.4f, CreateHelloHandPose("TY_hold", new Vector3(0f, 0.14f, -0.05f), new Vector3(-70f, 0f, 10f)))
                }
            };
        }

        /// <summary>
        /// Thank You (mano izquierda): Igual pero con rotación Z invertida (espejo).
        /// </summary>
        private static AnimatedPoseSequence CreateSignThankYouLeftHand()
        {
            return new AnimatedPoseSequence
            {
                poseName = "ThankYou_Left",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    // Inicio: pico invertido (Z negativo = dedos hacia la derecha = centro)
                    new PoseKeyframe(0f, CreateHelloHandPose("TYL_start", Vector3.zero, new Vector3(0f, 0f, -30f))),
                    new PoseKeyframe(0.3f, CreateHelloHandPose("TYL_mid", new Vector3(0f, 0.06f, 0f), new Vector3(-30f, 0f, -20f))),
                    new PoseKeyframe(0.7f, CreateHelloHandPose("TYL_end", new Vector3(0f, 0.14f, -0.05f), new Vector3(-70f, 0f, -10f))),
                    new PoseKeyframe(1.4f, CreateHelloHandPose("TYL_hold", new Vector3(0f, 0.14f, -0.05f), new Vector3(-70f, 0f, -10f)))
                }
            };
        }

        /// <summary>
        /// Please: Movimiento circular bien definido, dedos juntos y estirados (como B).
        /// 8 puntos en el círculo para suavidad, rápido.
        /// </summary>
        private static AnimatedPoseSequence CreateSignPlease()
        {
            float r = 0.04f;  // Radio del círculo
            float cz = 0.06f; // Centro Z
            float d = r * 0.707f; // cos(45°) para puntos diagonales

            return new AnimatedPoseSequence
            {
                poseName = "Please",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    // 8 puntos en el círculo (cada 45°) para movimiento suave
                    new PoseKeyframe(0f,    CreatePleaseHandPose("Pl_0", new Vector3(0f, 0f, cz + r))),     // arriba
                    new PoseKeyframe(0.07f, CreatePleaseHandPose("Pl_1", new Vector3(d, 0f, cz + d))),      // arriba-derecha
                    new PoseKeyframe(0.14f, CreatePleaseHandPose("Pl_2", new Vector3(r, 0f, cz))),           // derecha
                    new PoseKeyframe(0.21f, CreatePleaseHandPose("Pl_3", new Vector3(d, 0f, cz - d))),      // abajo-derecha
                    new PoseKeyframe(0.28f, CreatePleaseHandPose("Pl_4", new Vector3(0f, 0f, cz - r))),     // abajo
                    new PoseKeyframe(0.35f, CreatePleaseHandPose("Pl_5", new Vector3(-d, 0f, cz - d))),     // abajo-izquierda
                    new PoseKeyframe(0.42f, CreatePleaseHandPose("Pl_6", new Vector3(-r, 0f, cz))),          // izquierda
                    new PoseKeyframe(0.49f, CreatePleaseHandPose("Pl_7", new Vector3(-d, 0f, cz + d))),     // arriba-izquierda
                    new PoseKeyframe(0.56f, CreatePleaseHandPose("Pl_8", new Vector3(0f, 0f, cz + r))),     // arriba (cierra)
                    new PoseKeyframe(1.2f,  CreatePleaseHandPose("Pl_hold", new Vector3(0f, 0f, cz + r)))   // mantener
                }
            };
        }

        /// <summary>
        /// Good: Pulgar arriba con pequeño movimiento hacia arriba.
        /// </summary>
        private static AnimatedPoseSequence CreateSignGood()
        {
            return new AnimatedPoseSequence
            {
                poseName = "Good",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    new PoseKeyframe(0f, CreateThumbsUpPose(new Vector3(0.04f, 0f, 0.1f))),
                    new PoseKeyframe(0.3f, CreateThumbsUpPose(new Vector3(0.04f, 0f, 0.16f))),
                    new PoseKeyframe(1.0f, CreateThumbsUpPose(new Vector3(0.04f, 0f, 0.16f)))
                }
            };
        }

        /// <summary>
        /// Bad: Pulgar abajo con pequeño movimiento hacia abajo.
        /// </summary>
        private static AnimatedPoseSequence CreateSignBad()
        {
            return new AnimatedPoseSequence
            {
                poseName = "Bad",
                loop = false,
                keyframes = new PoseKeyframe[]
                {
                    new PoseKeyframe(0f, CreateThumbsDownPose(new Vector3(0.04f, 0f, 0.18f))),
                    new PoseKeyframe(0.3f, CreateThumbsDownPose(new Vector3(0.04f, 0f, 0.12f))),
                    new PoseKeyframe(1.0f, CreateThumbsDownPose(new Vector3(0.04f, 0f, 0.12f)))
                }
            };
        }

        /// <summary>
        /// Helper: crea una copia de una pose con wristPositionOffset modificado.
        /// </summary>
        private static HandPoseData WithWristOffset(HandPoseData basePose, string name, Vector3 wristPos)
        {
            return new HandPoseData
            {
                poseName = name,
                thumb = basePose.thumb,
                index = basePose.index,
                middle = basePose.middle,
                ring = basePose.ring,
                pinky = basePose.pinky,
                wristRotationOffset = basePose.wristRotationOffset,
                wristPositionOffset = wristPos
            };
        }

        /// <summary>
        /// Helper: crea una copia de una pose con wristRotationOffset y wristPositionOffset modificados.
        /// </summary>
        private static HandPoseData WithWristTransform(HandPoseData basePose, string name, Vector3 wristRot, Vector3 wristPos)
        {
            return new HandPoseData
            {
                poseName = name,
                thumb = basePose.thumb,
                index = basePose.index,
                middle = basePose.middle,
                ring = basePose.ring,
                pinky = basePose.pinky,
                wristRotationOffset = wristRot,
                wristPositionOffset = wristPos
            };
        }

        #endregion

        #endregion
    }
}
