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
                wristRotationOffset = new Vector3(0f, 180f, 10f),
                wristPositionOffset = new Vector3(0f, 0f, 0.2f),
                // Pulgar recto, paralelo al índice, pegado a él
                thumb = new ThumbPoseData
                {
                    metacarpalCurl = 0f,
                    proximalCurl = 0f,
                    distalCurl = 0f,
                    abductionAngle = 10f,          // Ligeramente hacia el índice
                    oppositionAngle = 15f,         // Cruzar hacia el índice para tocarlo
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
                    spreadAngle = -5f  // Separado del índice
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
                wristRotationOffset = new Vector3(0f, 180f, 40f),
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
                thumb = ThumbPoseData.ExtendedBeside,
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
            // Mano abierta con dedos separados naturalmente
            return new HandPoseData
            {
                poseName = "5",
                thumb = ThumbPoseData.ExtendedBeside,
                index = FingerPoseData.Extended,
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.Extended,
                pinky = FingerPoseData.Extended
            };
        }

        #endregion

        #region Animated Poses (J, Z)

        /// <summary>
        /// Obtiene una secuencia animada si la letra la requiere (J, Z).
        /// Retorna null si la letra no necesita animación.
        /// </summary>
        public static AnimatedPoseSequence GetAnimatedPose(string signName)
        {
            if (string.IsNullOrEmpty(signName))
                return null;

            return signName.ToUpper() switch
            {
                "J" => CreateLetterJ(),
                "Z" => CreateLetterZ(),
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
                    new PoseKeyframe(0.4f, new HandPoseData
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
                    new PoseKeyframe(0.7f, new HandPoseData
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
                    new PoseKeyframe(1.0f, new HandPoseData
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
                    new PoseKeyframe(1.5f, new HandPoseData
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
                        wristPositionOffset = new Vector3(0.04f, 0f, 0.04f)
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
                        wristPositionOffset = new Vector3(-0.04f, 0f, 0.04f)
                    }),
                    // Diagonal: abajo-derecha (en el mismo plano X-Z, izq→der diagonal)
                    new PoseKeyframe(1.0f, new HandPoseData
                    {
                        poseName = "Z_diag",
                        thumb = basePose.thumb,
                        index = basePose.index,
                        middle = basePose.middle,
                        ring = basePose.ring,
                        pinky = basePose.pinky,
                        wristRotationOffset = Vector3.zero,
                        wristPositionOffset = new Vector3(0.04f, 0f, -0.04f)
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
                        wristPositionOffset = new Vector3(-0.04f, 0f, -0.04f)
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
                        wristPositionOffset = new Vector3(-0.04f, 0f, -0.04f)
                    })
                }
            };
        }

        #endregion
    }
}
