using UnityEngine.UIElements;

namespace CottonCircuit
{
    // Authored static text: <ui:Label text="미리보기"><Bindings><CottonCircuit.LocalizedText property="text" key="title.tagline" /></Bindings></ui:Label>
    // Never put it on an element whose text C# assigns; the binding would overwrite it.
    [UxmlObject]
    public partial class LocalizedText : CustomBinding
    {
        [UxmlAttribute("key")] public string Key { get; set; }
        int appliedVersion = -1;

        public LocalizedText() { updateTrigger = BindingUpdateTrigger.EveryUpdate; }

        protected override BindingResult Update(in BindingContext context)
        {
            if (appliedVersion == Strings.Version) return new BindingResult(BindingStatus.Success);
            if (context.targetElement is TextElement text) text.text = Strings.Get(Key);
            appliedVersion = Strings.Version;
            return new BindingResult(BindingStatus.Success);
        }
    }
}
