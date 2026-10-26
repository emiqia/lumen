using UnityEngine;

namespace Lumen.Core
{
    /// <summary>
    /// References to the few engine assets that must survive build stripping
    /// (sprite materials). Filled in automatically by Lumen > Setup Project.
    /// </summary>
    [CreateAssetMenu(menuName = "Lumen/Assets", fileName = "LumenAssets")]
    public class LumenAssets : ScriptableObject
    {
        public Material spriteLit;
        public Material spriteUnlit;

        static LumenAssets instance;
        public static LumenAssets I
        {
            get
            {
                if (instance == null) instance = Resources.Load<LumenAssets>("LumenAssets");
                if (instance == null) instance = CreateInstance<LumenAssets>();
                return instance;
            }
        }

        public Material Lit => spriteLit != null ? spriteLit : (spriteLit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")));
        public Material Unlit => spriteUnlit != null ? spriteUnlit : (spriteUnlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")));
    }
}
