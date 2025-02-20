using Cysharp.Threading.Tasks;
using MajdataPlay.Collections;
using MajdataPlay.Extensions;
using MajdataPlay.Game;
using MajdataPlay.IO;
using MajdataPlay.Types;
using MajdataPlay.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.UI;

namespace MajdataPlay.List
{
    public class CoverListDisplayer : MonoBehaviour
    {
        public bool IsDirList => Mode == CoverListMode.Directory;
        public bool IsChartList => Mode == CoverListMode.Chart;
        public CoverListMode Mode { get; set; } = CoverListMode.Directory;

        List<CoverSmallDisplayer> covers = new List<CoverSmallDisplayer>();
        public string soundEffectName;
        public GameObject CoverSmallPrefab;
        public GameObject DirSmallPrefab;
        public GameObject DanSmallPrefab;
        public CoverBigDisplayer CoverBigDisplayer;
        public SubInfoDisplayer SubInfoDisplayer;
        public ChartAnalyzer chartAnalyzer;

        public int desiredListPos = 0;
        public float listPosReal;
        public float turnSpeed;
        public float radius;
        public float offset;
        public float angle;

        public int selectedDifficulty = 0;

        private SongCollection[] dirs = Array.Empty<SongCollection>();
        private SongCollection songs = new SongCollection();

        private SongLevelInCollection[] songLevelInCollections = Array.Empty<SongLevelInCollection>();
        // array of objects with song index, song level number, and song level 


        public void SwitchToDirList(SongCollection[] _dirs)
        {
            foreach (var cover in covers)
            {
                Destroy(cover.gameObject);
            }
            covers.Clear();
            Mode = CoverListMode.Directory;
            dirs = _dirs;
            desiredListPos = SongStorage.CollectionIndex;
            foreach (var dir in _dirs)
            {
                var prefab = DirSmallPrefab;
                if (dir.Type == ChartStorageType.Dan)
                {
                    prefab = DanSmallPrefab;
                }
                var obj = Instantiate(prefab, transform);
                var coversmall = obj.GetComponent<CoverSmallDisplayer>();
                //coversmall.SetCover(song.SongCover);
                coversmall.SetLevelText(dir.Name);
                covers.Add(coversmall);
                coversmall.gameObject.SetActive(false);
                coversmall.IsOnline = dir.IsOnline;
            }
            if (desiredListPos > covers.Count) desiredListPos = 0;
            listPosReal = desiredListPos;
            SlideToList(desiredListPos);
        }

        public void SwitchToSongList()
        {
            if (songs.Count == 0) return;
            if (songs.Type == ChartStorageType.Dan) return;
            foreach (var cover in covers)
            {
                Destroy(cover.gameObject);
            }
            covers.Clear();
            Mode = CoverListMode.Chart;
            desiredListPos = SongStorage.WorkingCollection.Index;
            for (var songIndex = 0; songIndex < songs.Count; songIndex++)
            {
                var song = songs[songIndex];
                Debug.Log("[x]" + string.Join(", ", song.Levels));
                for (var levelIndex = 0; levelIndex < song.Levels.Length; levelIndex++)
                {
                    var level = song.Levels[levelIndex];
                    if (level == null || level == "")
                    {
                        continue;
                    }
                    else
                    {
                        // add to songLevelInCollections
                        Array.Resize(ref songLevelInCollections, songLevelInCollections.Length + 1);
                        songLevelInCollections[songLevelInCollections.Length - 1] = new SongLevelInCollection(song.Levels[levelIndex], levelIndex, songIndex);

                        var obj = Instantiate(CoverSmallPrefab, transform);
                        var coversmall = obj.GetComponent<CoverSmallDisplayer>();
                        coversmall.SetOpacity(0f);
                        coversmall.SetCover(song);
                        coversmall.SetLevelText(song.Levels[levelIndex]);
                        covers.Add(coversmall);
                        coversmall.gameObject.SetActive(false);
                    }
                }
                //var obj = Instantiate(CoverSmallPrefab, transform);
                //var coversmall = obj.GetComponent<CoverSmallDisplayer>();
                //coversmall.SetOpacity(0f);
                //coversmall.SetCover(song);
                //coversmall.SetLevelText(song.Levels[selectedDifficulty]);
                //covers.Add(coversmall);
                //coversmall.gameObject.SetActive(false);
            }
            if (desiredListPos > covers.Count) desiredListPos = 0;
            listPosReal = desiredListPos;
            SlideToList(desiredListPos);
        }


        public void SlideDifficulty(int delta)
        {
            selectedDifficulty += delta;
            SlideToDifficulty(selectedDifficulty);
        }

        public void SlideToDifficulty(int pos)
        {
            selectedDifficulty = pos;
            if (selectedDifficulty > 6)
            {
                selectedDifficulty = 0;
            }
            if (selectedDifficulty < 0)
            {
                selectedDifficulty = 6;
            }
            MajInstances.GameManager.SelectedDiff = (ChartLevel)selectedDifficulty;
            var level = songLevelInCollections[desiredListPos];
            var songinfo = songs[level.SongCollectionIndex];
            // map level index to chart level
            var chartLevel = (ChartLevel)level.LevelIndex;
            CoverBigDisplayer.SetDifficulty(level.LevelIndex);
            if (IsChartList)
            {
                Debug.Log($"[CoverListDisplayer] desiredListPos: {desiredListPos}");
                Debug.Log($"[CoverListDisplayer] songLevelInCollections.length: {songLevelInCollections.Length}");

                var songScore = MajInstances.ScoreManager.GetScore(songinfo, chartLevel);
                CoverBigDisplayer.SetMeta(songinfo.Title, songinfo.Artist, songinfo.Designers[level.LevelIndex], songinfo.Levels[level.LevelIndex]);
                CoverBigDisplayer.SetScore(songScore);
                chartAnalyzer.AnalyzeSongDetail(songinfo, (ChartLevel)level.LevelIndex).Forget();

                //for (int i = 0; i < covers.Count; i++)
                //{
                //    var text = songs[i].Levels[selectedDifficulty];
                //    if (text == null || text == "") text = "-";
                //    covers[i].GetComponent<CoverSmallDisplayer>().SetLevelText(text);
                //}
            }
        }

        public void SlideList(int delta)
        {
            switch (Mode)
            {
                case CoverListMode.Directory:
                    SongStorage.CollectionIndex += delta;
                    desiredListPos = SongStorage.CollectionIndex;
                    break;
                case CoverListMode.Chart:
                    // var collection = SongStorage.WorkingCollection;
                    // collection.Move(delta);
                    ;
                    desiredListPos = (desiredListPos + delta).Clamp(0, songLevelInCollections.Length - 1);
                    break;
            }
            SlideToList(desiredListPos);
        }
        public void RefreshList()
        {
            var collection = SongStorage.WorkingCollection;
            desiredListPos = collection.Index;
            SlideToList(desiredListPos);
        }

        public void SlideToList(int pos)
        {
            MajInstances.AudioManager.PlaySFX("tap_perfect.wav");
            desiredListPos = pos;
            if (desiredListPos >= covers.Count)
            {

                desiredListPos = covers.Count - 1;
            }
            if (desiredListPos <= 0)
            {
                desiredListPos = 0;
            }
            // log covers count
            Debug.Log($"[CoverListDisplayer] SlideToList: {pos}, covers.Count: {covers.Count}");
            switch (Mode)
            {
                case CoverListMode.Directory:
                    songs = dirs[desiredListPos];
                    if (songs.Type == ChartStorageType.List)
                    {
                        CoverBigDisplayer.SetMeta(songs.Name, "Count:" + songs.Count, "", "");
                    }
                    else
                    {
                        CoverBigDisplayer.SetMeta(songs.DanInfo.Name, songs.DanInfo.Description, "", "");
                    }

                    CoverBigDisplayer.SetScore(new MaiScore());
                    SongStorage.CollectionIndex = desiredListPos;
                    break;
                case CoverListMode.Chart:
                    var level = songLevelInCollections[desiredListPos];
                    var songinfo = songs[level.SongCollectionIndex];
                    var chartLevel = (ChartLevel)level.LevelIndex;
                    var songScore = MajInstances.ScoreManager.GetScore(songinfo, chartLevel);
                    CoverBigDisplayer.SetCover(songinfo);
                    CoverBigDisplayer.SetMeta(songinfo.Title, songinfo.Artist, songinfo.Designers[level.LevelIndex], songinfo.Levels[level.LevelIndex]);
                    CoverBigDisplayer.SetScore(songScore);
                    CoverBigDisplayer.SetDifficulty(level.LevelIndex);

                    chartAnalyzer.AnalyzeSongDetail(songinfo, (ChartLevel)level.LevelIndex).Forget();

                    SubInfoDisplayer.RefreshContent(songinfo);
                    GetComponent<PreviewSoundPlayer>().PlayPreviewSound(songinfo);
                    SongStorage.WorkingCollection.Index = level.SongCollectionIndex;
                    MajInstances.GameManager.SelectedDiff = chartLevel;
                    break;
            }
        }
        private void FixedUpdate()
        {
            var delta = (desiredListPos - listPosReal) * turnSpeed;
            listPosReal += Mathf.Clamp(delta, -1f, 1f);
            if (Mathf.Abs(desiredListPos - listPosReal) < 0.01f) listPosReal = desiredListPos;
            for (int i = 0; i < covers.Count; i++)
            {
                var distance = i - listPosReal;
                var cover = covers[i];
                if (Mathf.Abs(distance) > 5)
                {
                    if (cover.gameObject.activeSelf)
                        cover.gameObject.SetActive(false);
                    continue;
                }
                if (!cover.gameObject.activeSelf)
                    cover.gameObject.SetActive(true);
                cover.RectTransform.anchoredPosition = GetCoverPosition(radius, (distance * angle - 90) * Mathf.Deg2Rad);
                if (Mathf.Abs(distance) > 4)
                {
                    cover.SetOpacity(-Mathf.Abs(distance) + 5);
                }
                else
                {
                    cover.SetOpacity(1f);
                }
            }
            if (IsDirList && Time.frameCount % 50 == 0)
            {
                if (songs.Count > 0)
                {
                    if (coveri >= songs.Count) coveri = 0;
                    CoverBigDisplayer.SetCover(songs[coveri++]);
                }
                else
                {
                    CoverBigDisplayer.SetNoCover();
                }
            }
        }
        private int coveri = 0;

        Vector3 GetCoverPosition(float radius, float position)
        {
            return new Vector3(radius * Mathf.Sin(position), radius * Mathf.Cos(position));
        }
    }
}