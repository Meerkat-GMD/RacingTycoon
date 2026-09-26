using UnityEngine;

namespace CottonCircuit
{
    public partial class GameController
    {
        [Header("설탕 흔들기")]
        [SerializeField, Range((float)SugarShake.MinFullStrokePixels, (float)SugarShake.MaxFullStrokePixels)]
        [InspectorName("10g 흔들기 폭 (px)")]
        [Tooltip("설탕 10g을 넣는 데 필요한 세로 흔들기 폭입니다. 값이 클수록 같은 동작에서 적게 들어갑니다. 기본값 132. 플레이 중 변경하면 다음 흔들기부터 적용됩니다.")]
        float sugarShakeFullStrokePixels = (float)SugarShake.DefaultFullStrokePixels;

        public double SugarShakeFullStrokePixels => SugarShake.NormalizeFullStrokePixels(sugarShakeFullStrokePixels);

        public void SetSugarShakeFullStrokePixels(double pixels)
        {
            sugarShakeFullStrokePixels = (float)SugarShake.NormalizeFullStrokePixels(pixels);
        }

        // OnValidate may run during loading: keep it free of scene/UI operations.
        void OnValidate() => SetSugarShakeFullStrokePixels(sugarShakeFullStrokePixels);
    }
}
