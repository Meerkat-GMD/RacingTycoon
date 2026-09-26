using System;
namespace CottonCircuit
{
    [Serializable] public class MachineProduction
    {
        public bool WorkerAssigned;
        public int RecipeFlavor, RecipeSize;
        public int SugarGrade = 1;
        public double SugarGrams;
        public int SugarFlavor = -1;
        public double BatchMeters;
        public int BatchFlavor = -1, BatchQuality = 50;
        public string BatchProductId;
        public int BatchSugarGrade = 1;
        public void Clear()
        {
            SugarGrams = 0; SugarFlavor = -1;
            ClearBatch();
        }
        public void ClearBatch()
        {
            BatchMeters = 0; BatchFlavor = -1;
            BatchQuality = 50; BatchProductId = null; BatchSugarGrade = 1;
        }
    }
}
