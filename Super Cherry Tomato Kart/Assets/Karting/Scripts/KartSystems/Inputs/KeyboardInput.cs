using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        float m_Acceleration;
        float m_Steering;
        bool m_HopPressed;
        bool m_HopHeld;
        bool m_BoostPressed;
        bool m_FirePressed;
        bool m_hopHeldLastFrame;

        bool m_FixedUpdateHappened;

        void Update ()
        {
            if (gameObject.name == "Player 1") {
                if (Game_Server.Player1_Buttons.Length == 9 && Game_Server.Player1_Buttons[0] == '1')
                    m_Acceleration = 1f;
                else if (Game_Server.Player1_Buttons.Length == 9 && Game_Server.Player1_Buttons[2] == '1')
                    m_Acceleration = -1f;
                else
                    m_Acceleration = 0f;

                if ((Game_Server.Player1_Buttons.Length == 9 && Game_Server.Player1_Buttons[4] == '1') && !(Game_Server.Player1_Buttons.Length == 9 && Game_Server.Player1_Buttons[6] == '1'))
                    m_Steering = -1f;
                else if (!(Game_Server.Player1_Buttons.Length == 9 && Game_Server.Player1_Buttons[4] == '1') && (Game_Server.Player1_Buttons.Length == 9 && Game_Server.Player1_Buttons[6] == '1'))
                    m_Steering = 1f;
                else
                    m_Steering = 0f;

                m_HopHeld = Game_Server.Player1_Buttons.Length == 9 && Game_Server.Player1_Buttons[8] == '1';
            }

            if (gameObject.name == "Player 2")
            {
                if (Game_Server.Player2_Buttons.Length == 9 && Game_Server.Player2_Buttons[0] == '1')
                    m_Acceleration = 1f;
                else if (Game_Server.Player2_Buttons.Length == 9 && Game_Server.Player2_Buttons[2] == '1')
                    m_Acceleration = -1f;
                else
                    m_Acceleration = 0f;

                if ((Game_Server.Player2_Buttons.Length == 9 && Game_Server.Player2_Buttons[4] == '1') && !(Game_Server.Player2_Buttons.Length == 9 && Game_Server.Player2_Buttons[6] == '1'))
                    m_Steering = -1f;
                else if (!(Game_Server.Player2_Buttons.Length == 9 && Game_Server.Player2_Buttons[4] == '1') && (Game_Server.Player2_Buttons.Length == 9 && Game_Server.Player2_Buttons[6] == '1'))
                    m_Steering = 1f;
                else
                    m_Steering = 0f;

                m_HopHeld = Game_Server.Player2_Buttons.Length == 9 && Game_Server.Player2_Buttons[8] == '1';
            }

            if (gameObject.name == "Player 3")
            {
                if (Game_Server.Player3_Buttons.Length == 9 && Game_Server.Player3_Buttons[0] == '1')
                    m_Acceleration = 1f;
                else if (Game_Server.Player3_Buttons.Length == 9 && Game_Server.Player3_Buttons[2] == '1')
                    m_Acceleration = -1f;
                else
                    m_Acceleration = 0f;

                if ((Game_Server.Player3_Buttons.Length == 9 && Game_Server.Player3_Buttons[4] == '1') && !(Game_Server.Player3_Buttons.Length == 9 && Game_Server.Player3_Buttons[6] == '1'))
                    m_Steering = -1f;
                else if (!(Game_Server.Player3_Buttons.Length == 9 && Game_Server.Player3_Buttons[4] == '1') && (Game_Server.Player3_Buttons.Length == 9 && Game_Server.Player3_Buttons[6] == '1'))
                    m_Steering = 1f;
                else
                    m_Steering = 0f;

                m_HopHeld = Game_Server.Player3_Buttons.Length == 9 && Game_Server.Player3_Buttons[8] == '1';
            }

            if (gameObject.name == "Player 4")
            {
                if (Game_Server.Player4_Buttons.Length == 9 && Game_Server.Player4_Buttons[0] == '1')
                    m_Acceleration = 1f;
                else if (Game_Server.Player4_Buttons.Length == 9 && Game_Server.Player4_Buttons[2] == '1')
                    m_Acceleration = -1f;
                else
                    m_Acceleration = 0f;

                if ((Game_Server.Player4_Buttons.Length == 9 && Game_Server.Player4_Buttons[4] == '1') && !(Game_Server.Player4_Buttons.Length == 9 && Game_Server.Player4_Buttons[6] == '1'))
                    m_Steering = -1f;
                else if (!(Game_Server.Player4_Buttons.Length == 9 && Game_Server.Player4_Buttons[4] == '1') && (Game_Server.Player4_Buttons.Length == 9 && Game_Server.Player4_Buttons[6] == '1'))
                    m_Steering = 1f;
                else
                    m_Steering = 0f;

                m_HopHeld = Game_Server.Player4_Buttons.Length == 9 && Game_Server.Player4_Buttons[8] == '1';
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
            m_FixedUpdateHappened = true;
        }
    }
}