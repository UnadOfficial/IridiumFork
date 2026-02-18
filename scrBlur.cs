using System;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000080 RID: 128
public class scrBlur : ADOBase
{
	// Token: 0x060002FC RID: 764 RVA: 0x0004A4FC File Offset: 0x000486FC
	private void Init()
	{
		this.rawImage = base.GetComponent<RawImage>();
		int width = this.rawImage.texture.width;
		int height = this.rawImage.texture.height;
		this.blurMaterial = new Material(ADOBase.gc.tileBlurShader);
		this.destTexture = new RenderTexture(width, height, 0);
		this.destTexture.Create();
		this.init = true;
	}

	// Token: 0x060002FD RID: 765 RVA: 0x0004A570 File Offset: 0x00048770
	public void UpdateTexture()
	{
		if (!this.init)
		{
			this.Init();
		}
		this.texture = this.rawImage.texture;
		this.blurMaterial.SetTexture("_BaseTex", this.texture);
		this.blurMaterial.SetTexture("_TileTex", this.tileTexture);
		this.blurMaterial.SetColor("_BaseTint", this.baseTint);
		this.blurMaterial.SetColor("_BlurTint", this.blurTint);
		this.blurMaterial.SetFloat("_Tinting", this.tinting);
		this.blurMaterial.SetFloat("_BlurSize", this.blurSize);
		this.rawImage.texture = this.BlurTexture(this.texture);
	}

	// Token: 0x060002FE RID: 766 RVA: 0x0004A638 File Offset: 0x00048838
	public Texture BlurTexture(Texture sourceTexture)
	{
		RenderTexture active = RenderTexture.active;
		try
		{
			RenderTexture temporary = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height);
			RenderTexture temporary2 = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height);
			for (int i = 0; i < this.passes; i++)
			{
				this.blurMaterial.SetFloat("_Pass", (float)i);
				Graphics.Blit((i == 0) ? sourceTexture : temporary2, temporary, this.blurMaterial, 0);
				Graphics.Blit(temporary, temporary2, this.blurMaterial, 1);
			}
			Graphics.Blit(temporary2, this.destTexture, this.blurMaterial, 2);
			RenderTexture.ReleaseTemporary(temporary);
			RenderTexture.ReleaseTemporary(temporary2);
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
		}
		finally
		{
			RenderTexture.active = active;
		}
		return this.destTexture;
	}

	// Token: 0x0400045B RID: 1115
	public Texture2D tileTexture;

	// Token: 0x0400045C RID: 1116
	[Header("Variables")]
	public float tinting = 0.4f;

	// Token: 0x0400045D RID: 1117
	public float blurSize = 4f;

	// Token: 0x0400045E RID: 1118
	public int passes = 8;

	// Token: 0x0400045F RID: 1119
	public Color baseTint;

	// Token: 0x04000460 RID: 1120
	public Color blurTint;

	// Token: 0x04000461 RID: 1121
	public Texture texture;

	// Token: 0x04000462 RID: 1122
	public Material blurMaterial;

	// Token: 0x04000463 RID: 1123
	public RenderTexture destTexture;

	// Token: 0x04000464 RID: 1124
	private bool init;

	// Token: 0x04000465 RID: 1125
	private RawImage rawImage;
}
