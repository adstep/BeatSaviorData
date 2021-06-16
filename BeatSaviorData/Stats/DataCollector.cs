using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BS_Utils.Utilities;
using UnityEngine;
using static BeatmapSaveData;

namespace BeatSaviorData
{
	public class DataCollector
	{
		public List<Note> notes = new List<Note>();
		public int maxCombo, bombHit, nbOfPause, nbOfWallHit;

		private int combo;
		private ScoreController sc;
        private MemoryPoolContainer<GameNoteController> notePool;
		private PlayerHeadAndObstacleInteraction phaoi;

		public void RegisterCollector(SongData data)
		{
			Note.ResetID();
			SwingTranspilerHandler.Reset();

			var recorder = Resources.FindObjectsOfTypeAll<BeatmapObjectExecutionRatingsRecorder>().FirstOrDefault();
            var objectManager = recorder.GetPrivateField<BeatmapObjectManager>("_beatmapObjectManager");

            if (objectManager is BasicBeatmapObjectManager)
            {
                Logger.log.Info($"BSD : Found BasicBeatmapObjectManager");
				notePool = objectManager.GetPrivateField<MemoryPoolContainer<GameNoteController>>("_gameNotePoolContainer");
            }
            else
            {
                Logger.log.Warn($"BSD : Unable to find BasicBeatmapObjectManager. Instead found '{objectManager.GetType().Name}'");
			}

            sc = data.GetScoreController();
			data.GetScoreController().noteWasCutEvent += OnNoteCut;
			data.GetScoreController().noteWasMissedEvent += OnNoteMiss;
			data.GetScoreController().comboBreakingEventHappenedEvent += BreakCombo;
			BS_Utils.Utilities.BSEvents.songPaused += SongPaused;
		}

		public void UnregisterCollector(SongData data)
		{
			if (combo > maxCombo)
				maxCombo = combo;

			data.GetScoreController().noteWasCutEvent -= OnNoteCut;
			data.GetScoreController().noteWasMissedEvent -= OnNoteMiss;
			data.GetScoreController().comboBreakingEventHappenedEvent -= BreakCombo;
			BS_Utils.Utilities.BSEvents.songPaused -= SongPaused;
		}

		private void OnNoteCut(NoteData data, in NoteCutInfo info, int multiplier)
        {
            var activeNotes = notePool.activeItems;

            GameNoteController foundNote = null;

            foreach (var activeNote in activeNotes)
            {
                if (activeNote.noteData == data)
                {
                    foundNote = activeNote;	
                    break;
                }
            }

            if (foundNote != null)
            {
                var notePosition = foundNote.transform.position;
                Logger.log.Info($"BSD : Cut note at {notePosition.x:G9}");
			}
            else
            {
				Logger.log.Info($"BSD : Failed to find GameNoteController for note");
			}

			// (data.colorType != ColorType.None) checks if it is not a bomb
			if (info.allIsOK && data.colorType != ColorType.None)
			{
				combo++;
				notes.Add(new Note(data, CutType.cut, info, multiplier));
			}
			else if (data.colorType != ColorType.None)
			{
				notes.Add(new Note(data, CutType.badCut, info, multiplier));
			} 
			else if (data.colorType == ColorType.None)
			{
				bombHit++;
			}
		}

		private void OnNoteMiss(NoteData data, int multiplier)
		{
			if (data.colorType != ColorType.None)
			{
				notes.Add(new Note(data, CutType.miss, multiplier));
			}
		}

		private void BreakCombo()
		{
			phaoi = phaoi ?? sc.GetField<PlayerHeadAndObstacleInteraction, ScoreController>("_playerHeadAndObstacleInteraction");

			if (phaoi != null && phaoi.intersectingObstacles.Count > 0)
				nbOfWallHit++;

			if (combo > maxCombo)
				maxCombo = combo;
			combo = 0;
		}

		private void SongPaused()
		{
			nbOfPause++;
		}

	}
}
