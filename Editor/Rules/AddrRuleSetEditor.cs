using UnityEditor;
using UnityEngine;

namespace AddrKit.Editor
{
    /// <summary>Inspector for a rule set: the same rule cards as the AddrKit window, plus Apply.</summary>
    [CustomEditor(typeof(AddrRuleSet))]
    public sealed class AddrRuleSetEditor : UnityEditor.Editor
    {
        readonly RuleSetView view = new RuleSetView();

        public override void OnInspectorGUI()
        {
            view.Draw((AddrRuleSet)target);

            GUILayout.Space(4);
            if (AddrGUI.PrimaryButton("Apply Rules")) AddrKitSetup.ApplyRules();
        }
    }
}
