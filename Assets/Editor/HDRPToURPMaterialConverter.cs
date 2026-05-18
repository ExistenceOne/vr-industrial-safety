using UnityEngine;
using UnityEditor;

public static class HDRPToURPMaterialConverter
{
    // 메뉴: Tools/HDRP → URP/Force Convert Background Materials
    [MenuItem("Tools/HDRP → URP/Force Convert Background Materials")]
    public static void ForceConvertBackgroundMaterials()
    {
        // 이 폴더 안의 머티리얼 전부를 대상으로 함
        string folder = "Assets/UnityFactorySceneHDRP/Scene_Factory/Background/Materials";

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        int converted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // 1) 기존 셰이더/텍스처 정보 읽어오기
            Shader oldShader = mat.shader;
            string shaderName = oldShader != null ? oldShader.name : "(null)";

            // 알베도 후보들 중 하나라도 있으면 사용
            Texture albedo =
                mat.GetTexture("_BaseColorMap") ??
                mat.GetTexture("_BaseMap") ??
                mat.GetTexture("_MainTex");

            Color albedoColor = Color.white;
            if (mat.HasProperty("_BaseColor"))
                albedoColor = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
                albedoColor = mat.GetColor("_Color");

            // 노멀 후보
            Texture normal =
                mat.GetTexture("_NormalMap") ??
                mat.GetTexture("_BumpMap");

            // 에미션 후보
            Texture emission =
                mat.GetTexture("_EmissiveColorMap") ??
                mat.GetTexture("_EmissionMap");
            Color emissionColor = Color.black;
            if (mat.HasProperty("_EmissiveColor"))
                emissionColor = mat.GetColor("_EmissiveColor");
            else if (mat.HasProperty("_EmissionColor"))
                emissionColor = mat.GetColor("_EmissionColor");

            // 2) 타깃 셰이더 선택 (URP 있으면 URP/Lit, 아니면 Standard)
            Shader target = Shader.Find("Universal Render Pipeline/Lit");
            bool useURP = true;

            if (target == null)
            {
                target = Shader.Find("Standard");
                useURP = false;
            }

            if (target == null)
            {
                Debug.LogError("URP Lit / Standard 셰이더를 찾을 수 없습니다. 프로젝트 렌더 파이프라인을 확인하세요.");
                return;
            }

            mat.shader = target;

            // 3) 새 셰이더 프로퍼티에 값 세팅
            if (useURP)
            {
                // URP/Lit
                if (albedo != null)
                    mat.SetTexture("_BaseMap", albedo);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", albedoColor);

                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                }

                if (emission != null)
                {
                    mat.SetTexture("_EmissionMap", emission);
                    mat.EnableKeyword("_EMISSION");
                }
                if (emissionColor != Color.black && mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emissionColor);
                    mat.EnableKeyword("_EMISSION");
                }
            }
            else
            {
                // Built-in Standard
                if (albedo != null)
                    mat.SetTexture("_MainTex", albedo);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", albedoColor);

                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                }

                if (emission != null)
                {
                    mat.SetTexture("_EmissionMap", emission);
                    mat.EnableKeyword("_EMISSION");
                }
                if (emissionColor != Color.black && mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emissionColor);
                    mat.EnableKeyword("_EMISSION");
                }
            }

            EditorUtility.SetDirty(mat);
            converted++;
            // Debug.Log($"Converted {path} (from {shaderName})");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[HDRP→URP] Background Materials 변환 완료: {converted}개 머티리얼 처리.");
    }
}
