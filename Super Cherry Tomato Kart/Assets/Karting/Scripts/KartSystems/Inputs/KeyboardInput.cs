using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KartGame.KartSystems
{
    /// <summary>
    /// A basic keyboard implementation of the IInput interface for all the input information a kart needs.
    /// </summary>
    public class KeyboardInput : MonoBehaviour, IInput
    {
        public float Acceleration
        {
            get { return m_Acceleration; }
        }
        public float Steering
        {
            get { return m_Steering; }
        }
        public bool BoostPressed
        {
            get { return m_BoostPressed; }
        }
        public bool FirePressed
        {
            get { return m_FirePressed; }
        }
        public bool HopPressed
        {
            get { return m_HopPressed; }
        }
        public bool HopHeld
        {
            get { return m_HopHeld; }
        }

        public MultiplicativeKartModifier boostStats;
        public MultiplicativeKartModifier shrinkStats;
        public MultiplicativeKartModifier stopStats;

        public ParticleSystem PowerUp;
        public static string PowerUpObtained = "noone";

        float m_Acceleration;
        float m_Steering;
        bool m_HopPressed;
        bool m_HopHeld;
        bool m_BoostPressed;
        bool m_FirePressed;
        bool m_hopHeldLastFrame;
        bool m_HasPowerUp = false;
        bool m_protected_from_shrink = false;
        bool m_is_stopped = false;
        bool m_pushed_power_up = false;
        bool m_is_invincible = false;

        bool m_FixedUpdateHappened;
        static bool m_shrink_activated = false;
        int m_frames_since_activated;
        private Vector3 minimized = new Vector3(0.25f, 0.25f, 0.25f);
        private Vector3 normal = new Vector3(1f, 1f, 1f);

        IEnumerator KartModifier(KartGame.KartSystems.KartMovement kart, float lifetime)
        {
            kart.AddKartModifier(boostStats);
            yield return new WaitForSeconds(lifetime);
            kart.RemoveKartModifier(boostStats);
            PowerUpObtained = "used";
        }

        IEnumerator PaintModifier(float lifetime)
        {
            
            foreach (Camera c in Camera.allCameras)
            {
                if (c.name != gameObject.name) {
                    c.fieldOfView = 0;
                    Debug.Log(c.name);
                }
            }
            
            yield return new WaitForSeconds(lifetime);

            foreach (Camera c in Camera.allCameras)
            {
                c.fieldOfView = 40;
            }
            PowerUpObtained = "used";
        }

        IEnumerator StopModifier(KartGame.KartSystems.KartMovement kart, float lifetime)
        {
            if (!m_is_invincible)
            {
                kart.AddKartModifier(stopStats);
                yield return new WaitForSeconds(lifetime);
                kart.RemoveKartModifier(stopStats);
                m_is_stopped = false;
            } else
            {
                Debug.Log(gameObject.name + ": invincibility protected from shrink and has now worn off");
                m_is_invincible = false;
            }

            PowerUpObtained = "used";
        }

        IEnumerator ShrinkModifier(KartGame.KartSystems.KartMovement kart, GameObject gameObject, float lifetime)
        {
            gameObject.transform.localScale = minimized;
            kart.AddKartModifier(shrinkStats);
            yield return new WaitForSeconds(lifetime);
            gameObject.transform.localScale = normal;
            kart.RemoveKartModifier(shrinkStats);
            m_shrink_activated = false;
            PowerUpObtained = "used";
        }

        void HandlePowerUp() {
            if (m_HasPowerUp && m_pushed_power_up)
            {
                if (UVoiceRec.quick_registered) //Replace later
                {
                    UVoiceRec.quick_registered = false;
                    float duration = 1f;
                    var kart = gameObject.GetComponent<KartMovement>();
                    kart.StartCoroutine(KartModifier(kart, duration));
                    m_HasPowerUp = false;
                }

                if (UVoiceRec.stop_registered) //Replace later
                {
                    UVoiceRec.stop_registered = false;
                    m_is_stopped = true;
                    float duration = 2.5f;
                    var kart = gameObject.GetComponent<KartMovement>();
                    kart.StartCoroutine(StopModifier(kart, duration));
                    m_HasPowerUp = false;
                }

                if (UVoiceRec.invincible_registered) //Replace later
                {
                    UVoiceRec.invincible_registered = false;
                    var kart = gameObject.GetComponent<KartMovement>();
                    m_is_invincible = true;
                    m_HasPowerUp = false;
                    
                }

                if (UVoiceRec.paint_registered) //Replace later
                {
                    UVoiceRec.paint_registered = false;
                    float duration = 4f;
                    var kart = gameObject.GetComponent<KartMovement>();
                    kart.StartCoroutine(PaintModifier(duration));
                    m_HasPowerUp = false;
                }

                if (UVoiceRec.shrink_registered) //Replace later
                {
                    UVoiceRec.shrink_registered = false;
                    m_shrink_activated = true;
                    m_protected_from_shrink = true;
                    m_frames_since_activated = 0;
                    m_HasPowerUp = false;
                }
            }
        }

        void Update ()
        {
            if (Input.GetKey(KeyCode.R))
            {
                SceneManager.LoadScene("contrast");
            }

            if (SceneManager.GetActiveScene().name == "contrast")
                return;
           
            if (m_HasPowerUp && !PowerUp.isPlaying)
            {
                PowerUp.Play();
            }
            else if(!m_HasPowerUp && PowerUp.isPlaying)
            {
                PowerUp.Stop();
            }

            if (gameObject.name == "Player 1") {
                
                if ((Game_Server.Player1_Buttons.Length == 11 && Game_Server.Player1_Buttons[0] == '1') || Input.GetKey(KeyCode.UpArrow) && !m_is_stopped)
                    m_Acceleration = 1f;
                else if (Game_Server.Player1_Buttons.Length == 11 && Game_Server.Player1_Buttons[2] == '1' || Input.GetKey(KeyCode.DownArrow) && !m_is_stopped)
                    m_Acceleration = -1f;
                else
                    m_Acceleration = 0f;

                if ((Game_Server.Player1_Buttons.Length == 11 && Game_Server.Player1_Buttons[4] == '1') || Input.GetKey(KeyCode.LeftArrow) && !(Game_Server.Player1_Buttons.Length == 11 && Game_Server.Player1_Buttons[6] == '1'))
                    m_Steering = -1f;
                else if (!(Game_Server.Player1_Buttons.Length == 11 && Game_Server.Player1_Buttons[4] == '1')  && (Game_Server.Player1_Buttons.Length == 11 && Game_Server.Player1_Buttons[6] == '1') || Input.GetKey(KeyCode.DownArrow))
                    m_Steering = 1f;
                else
                    m_Steering = 0f;

                m_HopHeld = Game_Server.Player1_Buttons.Length == 11 && Game_Server.Player1_Buttons[8] == '1';

                m_pushed_power_up = Game_Server.Player1_Buttons.Length == 11 && Game_Server.Player1_Buttons[10] == '1';

                if (PowerUpObtained == "Player 1")
                {
                    PowerUpObtained = "active";
                    m_HasPowerUp = true;
                }
            }

            if (gameObject.name == "Player 2")
            {

                if ((Game_Server.Player2_Buttons.Length == 11 && Game_Server.Player2_Buttons[0] == '1') || Input.GetKey(KeyCode.W) && !m_is_stopped)
                    m_Acceleration = 1f;
                else if (Game_Server.Player2_Buttons.Length == 11 && Game_Server.Player2_Buttons[2] == '1' && !m_is_stopped)
                    m_Acceleration = -1f;
                else
                    m_Acceleration = 0f;

                if ((Game_Server.Player2_Buttons.Length == 11 && Game_Server.Player2_Buttons[4] == '1') && !(Game_Server.Player2_Buttons.Length == 11 && Game_Server.Player2_Buttons[6] == '1'))
                    m_Steering = -1f;
                else if (!(Game_Server.Player2_Buttons.Length == 11 && Game_Server.Player2_Buttons[4] == '1') && (Game_Server.Player2_Buttons.Length == 11 && Game_Server.Player2_Buttons[6] == '1'))
                    m_Steering = 1f;
                else
                    m_Steering = 0f;

                m_HopHeld = Game_Server.Player2_Buttons.Length == 11 && Game_Server.Player2_Buttons[8] == '1';

                m_pushed_power_up = Game_Server.Player2_Buttons.Length == 11 && Game_Server.Player2_Buttons[10] == '1';

                if (PowerUpObtained == "Player 2")
                {
                    PowerUpObtained = "active";
                    m_HasPowerUp = true;
                }
            }

            if (gameObject.name == "Player 3")
            {
                if (Game_Server.Player3_Buttons.Length == 11 && Game_Server.Player3_Buttons[0] == '1' && !m_is_stopped)
                    m_Acceleration = 1f;
                else if (Game_Server.Player3_Buttons.Length == 11 && Game_Server.Player3_Buttons[2] == '1' && !m_is_stopped)
                    m_Acceleration = -1f;
                else
                    m_Acceleration = 0f;

                if ((Game_Server.Player3_Buttons.Length == 11 && Game_Server.Player3_Buttons[4] == '1') && !(Game_Server.Player3_Buttons.Length == 11 && Game_Server.Player3_Buttons[6] == '1'))
                    m_Steering = -1f;
                else if (!(Game_Server.Player3_Buttons.Length == 11 && Game_Server.Player3_Buttons[4] == '1') && (Game_Server.Player3_Buttons.Length == 11 && Game_Server.Player3_Buttons[6] == '1'))
                    m_Steering = 1f;
                else
                    m_Steering = 0f;

                m_HopHeld = Game_Server.Player3_Buttons.Length == 11 && Game_Server.Player3_Buttons[8] == '1';

                m_pushed_power_up = Game_Server.Player3_Buttons.Length == 11 && Game_Server.Player3_Buttons[10] == '1';

                if (PowerUpObtained == "Player 3")
                {
                    PowerUpObtained = "active";
                    m_HasPowerUp = true;
                }
            }

            if (gameObject.name == "Player 4")
            {
                if (Game_Server.Player4_Buttons.Length == 11 && Game_Server.Player4_Buttons[0] == '1' && !m_is_stopped)
                    m_Acceleration = 1f;
                else if (Game_Server.Player4_Buttons.Length == 11 && Game_Server.Player4_Buttons[2] == '1' && !m_is_stopped)
                    m_Acceleration = -1f;
                else
                    m_Acceleration = 0f;

                if ((Game_Server.Player4_Buttons.Length == 11 && Game_Server.Player4_Buttons[4] == '1') && !(Game_Server.Player4_Buttons.Length == 11 && Game_Server.Player4_Buttons[6] == '1'))
                    m_Steering = -1f;
                else if (!(Game_Server.Player4_Buttons.Length == 11 && Game_Server.Player4_Buttons[4] == '1') && (Game_Server.Player4_Buttons.Length == 11 && Game_Server.Player4_Buttons[6] == '1'))
                    m_Steering = 1f;
                else
                    m_Steering = 0f;

                m_HopHeld = Game_Server.Player4_Buttons.Length == 11 && Game_Server.Player4_Buttons[8] == '1';

                m_pushed_power_up = Game_Server.Player4_Buttons.Length == 11 && Game_Server.Player4_Buttons[10] == '1';

                if (PowerUpObtained == "Player 4")
                {
                    PowerUpObtained = "active";
                    m_HasPowerUp = true;
                }
            }

            HandlePowerUp();

            // Perform shrink
            if (m_shrink_activated && !m_protected_from_shrink)
            {
                m_protected_from_shrink = true;
                if (!m_is_invincible)
                {
                    Debug.Log("shrink activated!");
                    var kart = gameObject.GetComponent<KartMovement>();
                    kart.StartCoroutine(ShrinkModifier(kart, gameObject, 4.5f));
                } else
                {
                    Debug.Log(gameObject.name + ": invincibility protected from shrink and has now worn off");
                    m_is_invincible = false;
                }
            }

            if (m_FixedUpdateHappened)
            {
                m_FixedUpdateHappened = false;

                m_HopPressed = false;
                m_BoostPressed = false;
                m_FirePressed = false;
                m_hopHeldLastFrame = false;
            }

            m_HopPressed |= !m_hopHeldLastFrame && m_HopHeld;

            m_hopHeldLastFrame = m_HopHeld;
        }

        void FixedUpdate ()
        {
            m_frames_since_activated++;
            if (m_frames_since_activated == 230) {
                m_shrink_activated = false;
            }
            m_FixedUpdateHappened = true;
        }
    }
}