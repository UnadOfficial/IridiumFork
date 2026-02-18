using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using SA.GoogleDoc;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x020002FC RID: 764
public class scrHitText : MonoBehaviour
{
	// Token: 0x0600111E RID: 4382 RVA: 0x000A1A54 File Offset: 0x0009FC54
	public void Init(HitMargin hitMargin)
	{
		this.hitMargin = hitMargin;
		base.transform.parent.gameObject.SetActive(false);
		this.text = base.GetComponent<Text>();
		this.text.SetLocalizedFont();
		if (RDString.language != SystemLanguage.Korean && this.text.font == RDConstants.data.latinFont)
		{
			this.text.fontSize = Mathf.RoundToInt((float)this.text.fontSize * 1.1f);
		}
		this.dead = true;
		this.canvas = base.transform.parent;
		ColourSchemeHitMargin hitMarginColours = RDConstants.data.hitMarginColours;
		this.text.text = RDString.Get("HitMargin." + hitMargin.ToString(), null, LangSection.Translations);
		if (hitMargin == HitMargin.TooEarly)
		{
			this.text.color = hitMarginColours.colourTooEarly;
		}
		else if (hitMargin == HitMargin.VeryEarly)
		{
			this.text.color = hitMarginColours.colourVeryEarly;
		}
		else if (hitMargin == HitMargin.EarlyPerfect)
		{
			this.text.color = hitMarginColours.colourLittleEarly;
		}
		else if (hitMargin == HitMargin.Perfect)
		{
			this.text.color = hitMarginColours.colourPerfect;
		}
		else if (hitMargin == HitMargin.LatePerfect)
		{
			this.text.color = hitMarginColours.colourLittleLate;
		}
		else if (hitMargin == HitMargin.VeryLate)
		{
			this.text.color = hitMarginColours.colourVeryLate;
		}
		else if (hitMargin == HitMargin.TooLate)
		{
			this.text.color = hitMarginColours.colourTooLate;
		}
		else if (hitMargin == HitMargin.Multipress)
		{
			this.text.color = hitMarginColours.colourTooLate;
		}
		else if (hitMargin == HitMargin.OverPress)
		{
			this.text.color = hitMarginColours.colourFail;
		}
		if (GCS.bb)
		{
			this.text.fontSize = Mathf.RoundToInt((float)this.text.fontSize * 0.65f);
			if (hitMargin == HitMargin.EarlyPerfect || hitMargin == HitMargin.LatePerfect || hitMargin == HitMargin.Perfect)
			{
				this.text.color = hitMarginColours.colourPerfect;
			}
			else
			{
				this.text.color = hitMarginColours.colourTooLate;
			}
			this.outline.effectColor = Color.black;
			this.outline.useGraphicAlpha = false;
			this.outline.effectDistance = new Vector2(1f, 1f);
		}
		else
		{
			this.outline.useGraphicAlpha = false;
			this.outline.effectDistance = new Vector2(1f, 1f);
		}
		scrController instance = scrController.instance;
		this.gameCam = instance.camy.GetComponent<Camera>();
		this.forceOnScreen = instance.forceHitTextOnScreen;
		this.minBorderDistance = instance.hitTextMinBorderDistance;
	}

	// Token: 0x0600111F RID: 4383 RVA: 0x000A1CE8 File Offset: 0x0009FEE8
	public void Show(Vector3 position, float angle = 0f)
	{
		this.frameShown = Time.frameCount;
		this.timer = 0f;
		this.canvas.localPosition = position;
		this.canvas.gameObject.SetActive(true);
		this.dead = false;
		this.text.DOKill(false);
		this.text.color = this.text.color.WithAlpha(1f);
		this.text.DOFade(0f, 0.7f).SetDelay(0.5f).SetEase(Ease.OutQuad);
		scrMisc.Rotate2D(base.transform, scrController.instance.camy.transform.rotation.eulerAngles.z);
		base.transform.DOKill(false);
		base.transform.localScale = new Vector3(this.startingSize, this.startingSize, 1f);
		base.transform.DOPunchScale(new Vector3(this.sizeUp, this.sizeUp, 1f), this.duration, this.vibrato, this.elasticity);
		if (this.hitMargin != HitMargin.Perfect)
		{
			base.transform.DOLocalRotate(new Vector3(0f, 0f, angle * 20f), 2f, RotateMode.LocalAxisAdd);
		}
		if (GCS.bb)
		{
			this.canvas.localPosition = this.canvas.localPosition.WithY(1f);
			this.canvas.transform.eulerAngles = new Vector3(90f, 0f, 25f);
			this.text.GetComponent<Outline>().effectColor = Color.black;
			this.text.DOKill(false);
			this.text.color = this.text.color.WithAlpha(1f);
			this.text.DOFade(0f, 0.8f).SetDelay(0.25f).SetEase(Ease.OutQuad);
		}
		else
		{
			this.outline.effectColor = new Color(this.text.color.r * 0.3f, this.text.color.g * 0.3f, this.text.color.b * 0.3f, 1f);
			this.outline.DOFade(0f, 0.5f).SetDelay(0.25f).SetEase(Ease.OutQuad);
		}
		this.textPos = position;
	}

	// Token: 0x06001120 RID: 4384 RVA: 0x000A1F80 File Offset: 0x000A0180
	private void Update()
	{
		if (this.dead)
		{
			return;
		}
		if (this.forceOnScreen)
		{
			float num = this.gameCam.orthographicSize * 2f;
			float num2 = num * (float)Screen.width / (float)Screen.height;
			Vector3 position = this.gameCam.transform.position;
			Vector3 vector = this.textPos - position;
			Vector3 vector2 = this.textPos;
			vector2.x = position.x + Mathf.Clamp(vector.x, -num2 / 2f + this.minBorderDistance, num2 / 2f - this.minBorderDistance);
			vector2.y = position.y + Mathf.Clamp(vector.y, -num / 2f + this.minBorderDistance, num / 2f - this.minBorderDistance);
			this.canvas.localPosition = vector2;
		}
		this.timer += Time.deltaTime;
		if (this.timer > 1.25f)
		{
			this.dead = true;
			base.transform.DOKill(false);
			this.text.DOKill(false);
			base.transform.parent.gameObject.SetActive(false);
		}
	}

	// Token: 0x0400166C RID: 5740
	public Outline outline;

	// Token: 0x0400166D RID: 5741
	public HitMargin hitMargin;

	// Token: 0x0400166E RID: 5742
	public float startingSize;

	// Token: 0x0400166F RID: 5743
	public float sizeUp;

	// Token: 0x04001670 RID: 5744
	public float duration;

	// Token: 0x04001671 RID: 5745
	public int vibrato;

	// Token: 0x04001672 RID: 5746
	public float elasticity;

	// Token: 0x04001673 RID: 5747
	[NonSerialized]
	public bool dead;

	// Token: 0x04001674 RID: 5748
	private Text text;

	// Token: 0x04001675 RID: 5749
	private CanvasGroup sf;

	// Token: 0x04001676 RID: 5750
	private DOTweenAnimation anim;

	// Token: 0x04001677 RID: 5751
	private float timer;

	// Token: 0x04001678 RID: 5752
	private int frameShown;

	// Token: 0x04001679 RID: 5753
	private Transform canvas;

	// Token: 0x0400167A RID: 5754
	private bool forceOnScreen;

	// Token: 0x0400167B RID: 5755
	private float minBorderDistance;

	// Token: 0x0400167C RID: 5756
	private Camera gameCam;

	// Token: 0x0400167D RID: 5757
	private Vector3 textPos;
}
