using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using System.Linq;

namespace SilentClearWater.Unity
{
	public partial class ClearWaterInspector : ShaderGUI
	{

		sealed class DefaultStyles
		{
			public static GUIStyle scmStyle;
			public static GUIStyle sectionHeader;
			public static GUIStyle sectionHeaderBox;
            static DefaultStyles()
            {
				scmStyle = new GUIStyle("DropDownButton");
				sectionHeader = new GUIStyle(EditorStyles.miniBoldLabel);
				sectionHeader.padding.left = 24;
				sectionHeader.padding.right = -24;
				sectionHeaderBox = new GUIStyle( GUI.skin.box );
				sectionHeaderBox.alignment = TextAnchor.MiddleLeft;
				sectionHeaderBox.padding.left = 5;
				sectionHeaderBox.padding.right = -5;
				sectionHeaderBox.padding.top = 0;
				sectionHeaderBox.padding.bottom = 0;
			}
		}	

		sealed class HeaderExDecorator : MaterialPropertyDrawer
    	{
	        private readonly string header;

	        public HeaderExDecorator(string header)
	        {
	            this.header = header;
	        }

	        // so that we can accept Header(1) and display that as text
	        public HeaderExDecorator(float headerAsNumber)
	        {
	            this.header = headerAsNumber.ToString();
	        }

	        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
	        {
	            return 24f;
	        }

	        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
	        {/*
	            position.y += 8;
	            position = EditorGUI.IndentedRect(position);
	            GUI.Label(position, header, EditorStyles.boldLabel);
*/
            Rect r = position;
				r.x -= 2.0f;
				r.y += 2.0f;
				r.height = 18.0f;
				r.width -= 0.0f;
			GUI.Box(r, EditorGUIUtility.IconContent("d_FilterByType"), DefaultStyles.sectionHeaderBox);
			position.y += 2;
			GUI.Label(position, header, DefaultStyles.sectionHeader);
	        }
    	}

	    sealed class SetKeywordDrawer : MaterialPropertyDrawer
	    {
	        static bool s_drawing;

	        readonly string _keyword;

	        public SetKeywordDrawer() : this(default) { }

	        public SetKeywordDrawer(string keyword)
	        {
	            _keyword = keyword;
	        }

	        public override void Apply(MaterialProperty prop)
	        {
	            if (!string.IsNullOrEmpty(_keyword))
	            {
	                foreach (Material mat in prop.targets)
	                {
	                    if (mat.GetTexture(prop.name) != null)
	                        mat.EnableKeyword(_keyword);
	                    else
	                        mat.DisableKeyword(_keyword);
	                }
	            }
	        }

	        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
	            => 0;

	        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
	        {
	            if (s_drawing)
	            {
	                editor.DefaultShaderProperty(position, prop, label.text);
	            }
	            else if (prop.type == MaterialProperty.PropType.Texture)
	            {
	                var oldLabelWidth = EditorGUIUtility.labelWidth;
	                EditorGUIUtility.labelWidth = 0f;
	                s_drawing = true;
	                try
	                {
	                    EditorGUI.BeginChangeCheck();
	                    {
	                        editor.TextureProperty(prop, label.text);
	                    }
	                    if (EditorGUI.EndChangeCheck())
	                    {
	                        if (!string.IsNullOrEmpty(_keyword))
	                        {
	                            var useTexture = prop.textureValue != null;
	                            foreach (Material mat in prop.targets)
	                            {
	                                if (useTexture)
	                                    mat.EnableKeyword(_keyword);
	                                else
	                                    mat.DisableKeyword(_keyword);
	                            }
	                        }
	                    }
	                }
	                finally
	                {
	                    s_drawing = false;
	                    EditorGUIUtility.labelWidth = oldLabelWidth;
	                }
	            }
	        }
	    }

	// Used for toggling GrabPass
	internal class SetShaderPassToggleDrawer : MaterialPropertyDrawer
	{
		readonly string _passName;
    	readonly string _keyword;

		public SetShaderPassToggleDrawer() : this("Always", default) { }

		public SetShaderPassToggleDrawer(string passName, string keyword)
		{
			_passName = passName;
        	_keyword = keyword;
		}

		static bool IsPropertyTypeSuitable(MaterialProperty prop)
		{
			return prop.type == MaterialProperty.PropType.Float || prop.type == MaterialProperty.PropType.Range;
			// Not present in 2019.4
            // || prop.type == MaterialProperty.PropType.Int;
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			if (!IsPropertyTypeSuitable(prop))
			{
				return EditorGUIUtility.singleLineHeight * 2.5f;
			}
			return base.GetPropertyHeight(prop, label, editor);
		}

		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{

			EditorGUI.BeginChangeCheck();

			bool value = (Math.Abs(prop.floatValue) > 0.001f);
			EditorGUI.showMixedValue = prop.hasMixedValue;
			value = EditorGUI.Toggle(position, label, value);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				prop.floatValue = value ? 1.0f : 0.0f;
				SetShaderPassEnabled(prop, value);
			}
		}

		public override void Apply(MaterialProperty prop)
		{
			base.Apply(prop);
			if (!IsPropertyTypeSuitable(prop))
				return;

			if (prop.hasMixedValue)
				return;

			SetShaderPassEnabled(prop, (Math.Abs(prop.floatValue) > 0.001f));
		}

		protected virtual void SetShaderPassEnabled(MaterialProperty prop, bool on)
		{
			foreach (Material mat in prop.targets)
			{
				mat.SetShaderPassEnabled(_passName, on);
				if (on)
				{
					mat.EnableKeyword(_keyword);
				}
				else
				{
					mat.DisableKeyword(_keyword);
				}

			}
		}
	}



    	// From momoma's GeneLit, used with permission
    	// https://github.com/momoma-null/GeneLit
	    sealed class SingleLineDrawer : MaterialPropertyDrawer
	    {
	        static bool s_drawing;

	        readonly string _extraPropName;
	        readonly string _keyword;

	        public SingleLineDrawer() : this(default, default) { }

	        public SingleLineDrawer(string extraPropName) : this(extraPropName, default) { }

	        public SingleLineDrawer(string extraPropName, string keyword)
	        {
	            _extraPropName = extraPropName;
	            _keyword = keyword;
	        }

	        public override void Apply(MaterialProperty prop)
	        {
	            if (!string.IsNullOrEmpty(_keyword))
	            {
	                foreach (Material mat in prop.targets)
	                {
	                    if (mat.GetTexture(prop.name) != null)
	                        mat.EnableKeyword(_keyword);
	                    else
	                        mat.DisableKeyword(_keyword);
	                }
	            }
	        }

	        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
	            => 0;

	        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
	        {
	            if (s_drawing)
	            {
	                editor.DefaultShaderProperty(position, prop, label.text);
	            }
	            else if (prop.type == MaterialProperty.PropType.Texture)
	            {
	                var oldLabelWidth = EditorGUIUtility.labelWidth;
	                EditorGUIUtility.labelWidth = 0f;
	                s_drawing = true;
	                try
	                {
	                    EditorGUI.BeginChangeCheck();
	                    if (string.IsNullOrEmpty(_extraPropName))
	                    {
	                        editor.TexturePropertySingleLine(label, prop);
	                    }
	                    else
	                    {
	                        var extraProp = MaterialEditor.GetMaterialProperty(prop.targets, _extraPropName);
	                        if (extraProp.type == MaterialProperty.PropType.Color && (extraProp.flags & MaterialProperty.PropFlags.HDR) > 0)
	                            editor.TexturePropertyWithHDRColor(label, prop, extraProp, false);
	                        else
	                            editor.TexturePropertySingleLine(label, prop, extraProp);
	                    }
	                    if (EditorGUI.EndChangeCheck())
	                    {
	                        if (!string.IsNullOrEmpty(_keyword))
	                        {
	                            var useTexture = prop.textureValue != null;
	                            foreach (Material mat in prop.targets)
	                            {
	                                if (useTexture)
	                                    mat.EnableKeyword(_keyword);
	                                else
	                                    mat.DisableKeyword(_keyword);
	                            }
	                        }
	                    }
	                }
	                finally
	                {
	                    s_drawing = false;
	                    EditorGUIUtility.labelWidth = oldLabelWidth;
	                }
	            }
	        }
	    }

    	sealed class ScaleOffsetDecorator : MaterialPropertyDrawer
    	{
    	    bool _initialized = false;
	
    	    public ScaleOffsetDecorator() { }
	
    	    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    	    {
    	        if (!_initialized)
    	        {
    	            prop.ReplacePostDecorator(this);
    	            _initialized = true;
    	        }
    	        return 2f * EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    	    }
	
    	    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    	    {
    	        position.xMin += 15f;
    	        position.y = position.yMax - (2f * EditorGUIUtility.singleLineHeight + 2.5f * EditorGUIUtility.standardVerticalSpacing);
    	        editor.TextureScaleOffsetProperty(position, prop);
    	    }
    	}
    	
	    sealed class IfDefDecorator : MaterialPropertyDrawer
	    {
	        readonly string _keyword;

	        public IfDefDecorator(string keyword)
	        {
	            _keyword = keyword;
	        }

	        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
	        {
	            var materials = Array.ConvertAll(prop.targets, o => o as Material);
	            var enabled = materials[0].IsKeywordEnabled(_keyword);
	            if (!enabled)
	            {
	                prop.SkipRemainingDrawers(this);
	            }
	            else
	            {
	                for (var i = 1; i < materials.Length; ++i)
	                {
	                    if (materials[i].IsKeywordEnabled(_keyword) != enabled)
	                    {
	                        prop.SkipRemainingDrawers(this);
	                        break;
	                    }
	                }
	            }
	            return 0;
	        }

	        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
	        {
	            var materials = Array.ConvertAll(prop.targets, o => o as Material);
	            var enabled = materials[0].IsKeywordEnabled(_keyword);
	            if (!enabled)
	            {
	                prop.SkipRemainingDrawers(this);
	            }
	            else
	            {
	                for (var i = 1; i < materials.Length; ++i)
	                {
	                    if (materials[i].IsKeywordEnabled(_keyword) != enabled)
	                    {
	                        prop.SkipRemainingDrawers(this);
	                        break;
	                    }
	                }
	            }
	        }
	    }

	    sealed class IfNDefDecorator : MaterialPropertyDrawer
	    {
	        static readonly float s_helpBoxHeight = EditorStyles.helpBox.CalcHeight(GUIContent.none, 0f);

	        readonly string _keyword;

	        public IfNDefDecorator(string keyword)
	        {
	            _keyword = keyword;
	        }

	        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
	        {
	            var materials = Array.ConvertAll(prop.targets, o => o as Material);
	            var enabled = materials[0].IsKeywordEnabled(_keyword);
	            if (enabled)
	            {
	                prop.SkipRemainingDrawers(this);
	            }
	            else
	            {
	                for (var i = 1; i < materials.Length; ++i)
	                {
	                    if (materials[i].IsKeywordEnabled(_keyword) != enabled)
	                    {
	                        prop.SkipRemainingDrawers(this);
	                        break;
	                    }
	                }
	            }
	            return 0;
	        }

	        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
	        {
	            var materials = Array.ConvertAll(prop.targets, o => o as Material);
	            var enabled = materials[0].IsKeywordEnabled(_keyword);
	            if (enabled)
	            {
	                prop.SkipRemainingDrawers(this);
	            }
	            else
	            {
	                for (var i = 1; i < materials.Length; ++i)
	                {
	                    if (materials[i].IsKeywordEnabled(_keyword) != enabled)
	                    {
	                        prop.SkipRemainingDrawers(this);
	                        break;
	                    }
	                }
	            }
	        }
	    }

		

		public class CW3BlendModeSelectorDrawer : MaterialPropertyDrawer
		{
			private readonly string _srcBlend;
			private readonly string _dstBlend;
			private readonly string _customRenderQueue;
			private readonly string _zWrite;
			private readonly string _alphaToMask;
			
			public CW3BlendModeSelectorDrawer(string srcBlend, string dstBlend, string customRenderQueue, string zWrite = null, string alphaToMask = null)
			{
				_srcBlend = srcBlend;
				_dstBlend = dstBlend;
				_customRenderQueue = customRenderQueue;
				_zWrite = zWrite;
				_alphaToMask = alphaToMask;
			}

			private struct BlendModeData
			{
				public string name;
				public string keyword;
				public BlendMode srcBlend;
				public BlendMode dstBlend;
				public RenderQueue renderQueue;
				public string renderType;

				public BlendModeData(string name, string keyword, BlendMode srcBlend, BlendMode dstBlend, RenderQueue renderQueue, string renderType)
				{
					this.name = name;
					this.keyword = keyword;
					this.srcBlend = srcBlend;
					this.dstBlend = dstBlend;
					this.renderQueue = renderQueue;
					this.renderType = renderType;
				}
			}

			// Modified for Clear Water. 
			private static readonly BlendModeData[] blendModes = new BlendModeData[]
			{
				new BlendModeData("Transparent", "", BlendMode.One, BlendMode.OneMinusSrcAlpha, RenderQueue.Transparent, "Transparent"),
				new BlendModeData("Opaque", "", BlendMode.One, BlendMode.Zero, RenderQueue.Geometry, "Opaque"),
			};

			public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
			{
				EditorGUI.BeginChangeCheck();
				int blendMode = (int)prop.floatValue;
				string[] blendModeNames = System.Array.ConvertAll(blendModes, mode => mode.name);
				blendMode = EditorGUI.Popup(position, "Blend Mode", blendMode, blendModeNames);
				if (EditorGUI.EndChangeCheck())
				{
					editor.RegisterPropertyChangeUndo("Blend Mode");
					prop.floatValue = blendMode;
					foreach (var target in prop.targets)
					{
						SetMaterialBlendModeData((Material)target, blendMode);
					}
				}
			}
			
			private void SetMaterialBlendModeData(Material targetMat, int blendMode)
			{
				// Disable all keywords
				foreach (var mode in blendModes)
				{
					targetMat.DisableKeyword(mode.keyword);
				}

				// Enable the selected blend mode keyword and set the blend mode properties
				var data = blendModes[blendMode];
				targetMat.EnableKeyword(data.keyword);
				targetMat.SetInt(_srcBlend, (int)data.srcBlend);
				targetMat.SetInt(_dstBlend, (int)data.dstBlend);
				targetMat.SetOverrideTag("RenderType", data.renderType);

				// Set ZWrite based on blend mode and render queue
				if (!string.IsNullOrEmpty(_zWrite))
				{
					bool zWrite = ((blendMode == 0 || blendMode == 8) || targetMat.renderQueue < 3000);
					targetMat.SetInt(_zWrite, zWrite ? 1 : 0);
				}

				// Set AlphaToMask for Cutout mode
				if (!string.IsNullOrEmpty(_alphaToMask) && blendMode == 8)
				{
					targetMat.SetInt(_alphaToMask, 1);
				}
				else if (!string.IsNullOrEmpty(_alphaToMask))
				{
					targetMat.SetInt(_alphaToMask, 0);
				}

				// If the user has overridden the render queue, don't change it
				if (targetMat.HasProperty(_customRenderQueue))
				{
					if (targetMat.renderQueue == -1)
					{
						targetMat.SetInt(_customRenderQueue, -1);
					}
					int renderQueue = targetMat.GetInt(_customRenderQueue);
					targetMat.renderQueue = renderQueue > 1000 ? renderQueue : (int)data.renderQueue;
				}
				else
				{
					targetMat.renderQueue = (int)data.renderQueue;
				}
			}
		}
    }
}
