using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace CottonCircuit
{
    public class SaveStore
    {
        [Serializable] public class Envelope { public int Version = 1; public Economy State; }
        readonly string directory;
        string FilePath => Path.Combine(directory, "cotton-circuit.json");
        public string Error { get; private set; }
        public bool CanSave { get; private set; } = true;
        public SaveStore(string directory) { this.directory = directory; }
        public Economy Load()
        {
            if (!File.Exists(FilePath)) return new Economy();
            try
            {
                if (new FileInfo(FilePath).Length > 8 * 1024 * 1024) throw new InvalidDataException();
                var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(FilePath));
                if (envelope == null || envelope.Version != 1 || !Valid(envelope.State)) throw new InvalidDataException();
                return envelope.State;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
            { Error = "저장 파일을 읽지 못했어요. 원본을 보관하고 새로 시작할 수 있어요."; CanSave = false; return new Economy(); }
        }
        public bool Save(Economy state)
        {
            if (!CanSave) return false;
            try
            {
                Directory.CreateDirectory(directory);
                var temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(new Envelope { State = state }, true));
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
                else File.Move(temporary, FilePath);
                Error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Error = "저장하지 못했어요. 저장 폴더의 공간과 접근 권한을 확인해주세요."; return false; }
        }
        public bool ArchiveAndReset()
        {
            try
            {
                if (File.Exists(FilePath)) File.Move(FilePath, FilePath + ".archived-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
                CanSave = true; Error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Error = "기존 저장 파일을 보관하지 못했어요."; return false; }
        }
        public static bool Valid(Economy e)
        {
            if (e == null || e.Coins < 0 || e.Coins > 1000000000 || e.Day < 1 || e.Levels == null || e.Levels.Length != 3 ||
                e.Inventory == null || e.Inventory.Count > Economy.InventoryLimit || e.CompletedIds == null || e.TotalSold < 0 || e.LifetimeRevenue < 0) return false;
            foreach (int level in e.Levels) if (level < 0 || level > 3) return false;
            var ids = new HashSet<string>();
            foreach (var product in e.Inventory)
            {
                if (product == null || string.IsNullOrEmpty(product.Id) || !ids.Add(product.Id) || product.Samples == null ||
                    product.Samples.Count == 0 || product.Samples.Count > 230 || product.Grams != product.Samples.Count * 2) return false;
                double previous = 0;
                foreach (var sample in product.Samples)
                {
                    if (sample == null || sample.Flavor < 0 || sample.Flavor > 2 || !double.IsFinite(sample.Radius) || sample.Radius < 7 || sample.Radius > 13 ||
                        !double.IsFinite(sample.Angle) || sample.Angle <= previous || sample.Angle > 100) return false;
                    previous = sample.Angle;
                }
                if (!e.CompletedIds.Contains(product.Id)) e.CompletedIds.Add(product.Id);
            }
            return true;
        }
    }
}
