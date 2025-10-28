using UnityEngine;
using System.Collections.Generic;

namespace Antventure.UI
{
    /// <summary>
    /// Game Text Manager - Centralized management for all game text and hints
    /// </summary>
    [CreateAssetMenu(fileName = "GameTextManager", menuName = "Antventure/UI/Game Text Manager")]
    public class GameTextManager : ScriptableObject
    {
        [System.Serializable]
        public class HintTexts
        {
            [Header("Tutorial Hints")]
            [TextArea(2, 4)] public string tutorialStart = "Use WASD to move around";
            [TextArea(2, 4)] public string tutorialPickup = "Press C to pick up the sugar cube";
            [TextArea(2, 4)] public string tutorialDrop = "Press C again to drop the sugar cube";
            [TextArea(2, 4)] public string tutorialPressurePlate = "Place the sugar cube on the pressure plate";
            [TextArea(2, 4)] public string tutorialComplete = "You've completed the tutorial.\nBack to Home in {0}s";
            
            [Header("Checkpoint Hints")]
            [TextArea(2, 4)] public string checkpointActivated = "Checkpoint activated!";
            [TextArea(2, 4)] public string checkpointDefault = "Progress saved";
            
            [Header("Death Messages")]
            [TextArea(2, 4)] public string deathRespawn = "Left click to respawn";
            [TextArea(2, 4)] public string deathGeneral = "You died...";
            
            [Header("Energy/Resource Hints")]
            [TextArea(2, 4)] public string notEnoughEnergy = "Not enough energy (need 3 cookie crumbs)";
            [TextArea(2, 4)] public string energyCollected = "Cookie crumb collected!";
            
            [Header("Interaction Hints")]
            [TextArea(2, 4)] public string stoveSafe = "Stove is now safe";
            [TextArea(2, 4)] public string interactionPrompt = "Press E to interact";
            
            [Header("Level Completion")]
            [TextArea(2, 4)] public string levelComplete = "Level Complete!";
            [TextArea(2, 4)] public string congratulations = "Congratulations!";
        }
        
        [Header("Game Text Configuration")]
        public HintTexts hintTexts = new HintTexts();
        
        private static GameTextManager _instance;
        public static GameTextManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameTextManager>("GameTextManager");
                    if (_instance == null)
                    {
                        Debug.LogWarning("[GameTextManager] No GameTextManager found in Resources folder!");
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Get hint text by key
        /// </summary>
        public string GetHintText(HintTextKey key)
        {
            switch (key)
            {
                case HintTextKey.TutorialStart: return hintTexts.tutorialStart;
                case HintTextKey.TutorialPickup: return hintTexts.tutorialPickup;
                case HintTextKey.TutorialDrop: return hintTexts.tutorialDrop;
                case HintTextKey.TutorialPressurePlate: return hintTexts.tutorialPressurePlate;
                case HintTextKey.TutorialComplete: return hintTexts.tutorialComplete;
                case HintTextKey.CheckpointActivated: return hintTexts.checkpointActivated;
                case HintTextKey.CheckpointDefault: return hintTexts.checkpointDefault;
                case HintTextKey.DeathRespawn: return hintTexts.deathRespawn;
                case HintTextKey.DeathGeneral: return hintTexts.deathGeneral;
                case HintTextKey.NotEnoughEnergy: return hintTexts.notEnoughEnergy;
                case HintTextKey.EnergyCollected: return hintTexts.energyCollected;
                case HintTextKey.StoveSafe: return hintTexts.stoveSafe;
                case HintTextKey.InteractionPrompt: return hintTexts.interactionPrompt;
                case HintTextKey.LevelComplete: return hintTexts.levelComplete;
                case HintTextKey.Congratulations: return hintTexts.congratulations;
                default: return "Text not found";
            }
        }
        
        /// <summary>
        /// Get formatted hint text with parameters
        /// </summary>
        public string GetFormattedHintText(HintTextKey key, params object[] args)
        {
            string text = GetHintText(key);
            return string.Format(text, args);
        }
    }
    
    /// <summary>
    /// Hint text keys enumeration
    /// </summary>
    public enum HintTextKey
    {
        TutorialStart,
        TutorialPickup,
        TutorialDrop,
        TutorialPressurePlate,
        TutorialComplete,
        CheckpointActivated,
        CheckpointDefault,
        DeathRespawn,
        DeathGeneral,
        NotEnoughEnergy,
        EnergyCollected,
        StoveSafe,
        InteractionPrompt,
        LevelComplete,
        Congratulations
    }
}





