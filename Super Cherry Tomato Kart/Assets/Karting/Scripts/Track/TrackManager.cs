using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KartGame.KartSystems;
using UnityEngine;

namespace KartGame.Track
{
    /// <summary>
    /// A MonoBehaviour to deal with all the time and positions for the racers.
    /// </summary>
    public class TrackManager : MonoBehaviour
    {
        /*
         * player 1 is teal, player 2 is yellow, player 3 is red, player 4 is green
         * 
         */
        [Tooltip("The name of the track in this scene.  Used for track time records.  Must be unique.")]
        public string trackName;
        [Tooltip("Number of laps for the race.")]
        public int raceLapTotal;
        [Tooltip("All the checkpoints for the track in the order that they should be completed starting with the start/finish line checkpoint.")]
        public List<Checkpoint> checkpoints = new List<Checkpoint>();
        // goes from start/finish -> check1 -> check2 -> check3 and then loops 
        [Tooltip("Reference to an object responsible for repositioning karts.")]
        public KartRepositioner kartRepositioner;

        bool m_IsRaceRunning;
        Dictionary<IRacer, Checkpoint> m_RacerNextCheckpoints = new Dictionary<IRacer, Checkpoint>(16);
        // dictionary of 16 entries so 4 laps, 4 checkpoints each lap
        TrackRecord m_SessionBestLap = TrackRecord.CreateDefault();
        TrackRecord m_SessionBestRace = TrackRecord.CreateDefault();
        TrackRecord m_HistoricalBestLap;
        TrackRecord m_HistoricalBestRace;

        public int[] places = { 0, 0, 0, 0 };
        public bool IsRaceRunning => m_IsRaceRunning;
        public int firstPlace = 1;
        public float[] lapTimes = { 0.0f, 0.0f, 0.0f, 0.0f };
        public float[,] positions = new float[4, 2];
        public float[,] cp_pos = new float[4, 2];

        /// <summary>
        /// Returns the player number currently in first place.
        /// </summary>
        public string GetFirstPlace()
        {
            return firstPlace.ToString();
        }

        public void Update()
        {
            checkFirstPlace();
        }

        /// <summary>
        /// Returns the best lap time recorded this session.  If no record is found, -1 is returned.
        /// </summary>
        public float SessionBestLap
        {
            get
            {
                if (m_SessionBestLap != null && m_SessionBestLap.time < float.PositiveInfinity)
                    return m_SessionBestLap.time;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the best race time recorded this session.  If no record is found, -1 is returned.
        /// </summary>
        public float SessionBestRace
        {
            get
            {
                if (m_SessionBestRace != null && m_SessionBestRace.time < float.PositiveInfinity)
                    return m_SessionBestRace.time;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the best lap time ever recorded.  If no record is found, -1 is returned.
        /// </summary>
        public float HistoricalBestLap
        {
            get
            {
                if (m_HistoricalBestLap != null && m_HistoricalBestLap.time < float.PositiveInfinity)
                    return m_HistoricalBestLap.time;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the best race time ever recorded.  If no record is found, -1 is returned.
        /// </summary>
        public float HistoricalBestRace
        {
            get
            {
                if (m_HistoricalBestRace != null && m_HistoricalBestRace.time < float.PositiveInfinity)
                    return m_HistoricalBestRace.time;
                return -1f;
            }
        }

        void Awake()
        {
            if (checkpoints.Count < 3)
                Debug.LogWarning("There are currently " + checkpoints.Count + " checkpoints set on the Track Manager.  A minimum of 3 is recommended but kart control will not be enabled with 0.");

            m_HistoricalBestLap = TrackRecord.Load(trackName, 1);
            m_HistoricalBestRace = TrackRecord.Load(trackName, raceLapTotal);
        }

        void OnEnable()
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                checkpoints[i].OnKartHitCheckpoint += CheckRacerHitCheckpoint;
            }
        }

        void OnDisable()
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                checkpoints[i].OnKartHitCheckpoint -= CheckRacerHitCheckpoint;
            }
        }

        void Start()
        {
            if (checkpoints.Count == 0)
                return;

            Object[] allRacerArray = FindObjectsOfType<Object>().Where(x => x is IRacer).ToArray();

            for (int i = 0; i < allRacerArray.Length; i++)
            {
                IRacer racer = allRacerArray[i] as IRacer;
                m_RacerNextCheckpoints.Add(racer, checkpoints[0]);
                racer.DisableControl();
            }
            //print(m_RacerNextCheckpoints);

        }

        void checkFirstPlace()
        {
            //update positions for all
            foreach (KeyValuePair<IRacer, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints)
            {
                string name = racerNextCheckpoint.Key.GetName();
                int m = 0;
                switch (name)
                {
                    case "Player 1":
                        m = 0;
                        break;
                    case "Player 2":
                        m = 1;
                        break;
                    case "Player 3":
                        m = 2;
                        break;
                    case "Player 4":
                        m = 3;
                        break;
                }
                float[] position = racerNextCheckpoint.Key.GetPosition();
                positions[m, 0] = position[0];
                positions[m, 1] = position[1];
                float[] cp_position = racerNextCheckpoint.Value.GetPosition();
                cp_pos[m, 0] = cp_position[0];
                cp_pos[m, 1] = cp_position[1];
            }
            // now determine first place
            int index = 0;
            for (int i = 1; i < places.Length; i++)
            {
                if (places[i] > places[index])
                {
                    index = i;
                }
                else if (places[i] == places[index])
                {
                    double dif_i_x = positions[i, 0] - cp_pos[i, 0];
                    double dif_i_y = positions[i, 1] - cp_pos[i, 1];
                    double dif_index_x = positions[index, 0] - cp_pos[index, 0];
                    double dif_index_y = positions[index, 0] - cp_pos[index, 0];
                    // get the checkpoint for racer i and racer index
                    double dis_i = System.Math.Sqrt(System.Math.Pow(dif_i_x, 2.0)
                        + System.Math.Pow(dif_i_y, 2.0));
                    double dis_index = System.Math.Sqrt(System.Math.Pow(dif_index_x, 2.0)
                        + System.Math.Pow(dif_index_y, 2.0));
                    // then we need to tiebreak distance to next checkpoint
                    if (dis_i < dis_index)
                    {
                        // then current player is firstplace
                        index = i;
                    }
                    //otherwise nothing changes
                }
            }
            firstPlace = index + 1;
            //print("Current player in first place: " + firstPlace);
        }
        /// <summary>
        /// Starts the timers and enables control of all racers.
        /// </summary>
        public void StartRace()
        {
            m_IsRaceRunning = true;

            foreach (KeyValuePair<IRacer, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints)
            {
                racerNextCheckpoint.Key.EnableControl();
                racerNextCheckpoint.Key.UnpauseTimer();
            }
        }

        /// <summary>
        /// Stops the timers and disables control of all racers, also saves the historical records.
        /// </summary>
        public void StopRace()
        {
            m_IsRaceRunning = false;

            foreach (KeyValuePair<IRacer, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints)
            {
                racerNextCheckpoint.Key.DisableControl();
                racerNextCheckpoint.Key.PauseTimer();
            }

            TrackRecord.Save(m_HistoricalBestLap);
            TrackRecord.Save(m_HistoricalBestRace);
        }

        void add_point(IRacer racer, Checkpoint checkpoint)
        {
            // update values for each
            string name = racer.GetName();
            int m = 0;
            switch (name)
            {
                case "Player 1":
                    m = 0;
                    break;
                case "Player 2":
                    m = 1;
                    break;
                case "Player 3":
                    m = 2;
                    break;
                case "Player 4":
                    m = 3;
                    break;
            }
            places[m]++;
        }

        void CheckRacerHitCheckpoint(IRacer racer, Checkpoint checkpoint)
        {
            if (!m_IsRaceRunning)
            {
                StartCoroutine(CallWhenRaceStarts(racer, checkpoint));
                return;
            }
            // racer has hit the correct checkpoint
            if (m_RacerNextCheckpoints.ContainsKeyValuePair(racer, checkpoint))
            {
                add_point(racer, checkpoint);
                m_RacerNextCheckpoints[racer] = checkpoints.GetNextInCycle(checkpoint);
                RacerHitCorrectCheckpoint(racer, checkpoint);
            }
            else
            {
                // racer has not hit the correct checkpoint
                RacerHitIncorrectCheckpoint(racer, checkpoint);
            }
        }

        IEnumerator CallWhenRaceStarts(IRacer racer, Checkpoint checkpoint)
        {
            while (!m_IsRaceRunning)
            {
                yield return null;
            }

            CheckRacerHitCheckpoint(racer, checkpoint);
        }


        void RacerHitCorrectCheckpoint(IRacer racer, Checkpoint checkpoint)
        {
            if (checkpoint.isStartFinishLine)
            {
                int racerCurrentLap = racer.GetCurrentLap();
                if (racerCurrentLap > 0)
                {
                    float lapTime = racer.GetLapTime();

                    if (m_SessionBestLap.time > lapTime)
                        m_SessionBestLap.SetRecord(trackName, 1, racer, lapTime);

                    if (m_HistoricalBestLap.time > lapTime)
                        m_HistoricalBestLap.SetRecord(trackName, 1, racer, lapTime);

                    if (racerCurrentLap == raceLapTotal)
                    {
                        float raceTime = racer.GetRaceTime();

                        if (m_SessionBestRace.time > raceTime)
                            m_SessionBestRace.SetRecord(trackName, raceLapTotal, racer, raceTime);

                        if (m_HistoricalBestRace.time > raceTime)
                            m_HistoricalBestLap.SetRecord(trackName, raceLapTotal, racer, raceTime);

                        racer.DisableControl();
                        racer.PauseTimer();
                    }
                }

                if (CanEndRace())
                    StopRace();

                racer.HitStartFinishLine();
            }
        }

        bool CanEndRace()
        {
            foreach (KeyValuePair<IRacer, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints)
            {
                if (racerNextCheckpoint.Key.GetCurrentLap() < raceLapTotal)
                    return false;
            }

            return true;
        }

        void RacerHitIncorrectCheckpoint(IRacer racer, Checkpoint checkpoint)
        {
            // No implementation required by template.
        }

        /// <summary>
        /// This function should be called when a kart gets stuck or falls off the track.
        /// It will find the last checkpoint the kart went through and reposition it there.
        /// </summary>
        /// <param name="movable">The movable representing the kart.</param>
        public void ReplaceMovable(IMovable movable)
        {
            IRacer racer = movable.GetRacer();

            if (racer == null)
                return;

            Checkpoint nextCheckpoint = m_RacerNextCheckpoints[racer];
            int lastCheckpointIndex = (checkpoints.IndexOf(nextCheckpoint) + checkpoints.Count - 1) % checkpoints.Count;
            Checkpoint lastCheckpoint = checkpoints[lastCheckpointIndex];

            bool isControlled = movable.IsControlled();
            movable.DisableControl();
            kartRepositioner.OnRepositionComplete += ReenableControl;

            kartRepositioner.Reposition(lastCheckpoint, movable, isControlled);
        }

        void ReenableControl(IMovable movable, bool doEnableControl)
        {
            if (doEnableControl)
                movable.EnableControl();
            kartRepositioner.OnRepositionComplete -= ReenableControl;
        }
    }
}