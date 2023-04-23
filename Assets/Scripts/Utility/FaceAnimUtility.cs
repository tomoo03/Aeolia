using System;
using UnityEngine;

public class FaceAnimUtility {
  private const float NEGATIVE_MIN = -1.0f;
  private const float NEUTRAL_MIN = -0.05f;
  private const float NEUTRAL_MAX = 0.05f;
  private const float POSITIVE_MAX = 1.0f;
  public void ChangeFacialExpression(APIClient.AnalyzeResponse data) {
    var compound = data.compound;

    if (NEGATIVE_MIN <= compound && compound < NEUTRAL_MIN) {
      CallPlayFaceAnimation(FaceAnimationIndex.ANGRY_1);
    } else if (NEUTRAL_MIN <= compound && compound <= NEUTRAL_MAX) {
      CallPlayFaceAnimation(FaceAnimationIndex.DEFAULT);
    } else if (NEUTRAL_MAX < compound && compound <= POSITIVE_MAX) {
      CallPlayFaceAnimation(FaceAnimationIndex.SMILE_1);
    }
  }

  public void ResetToDefaultFace() {
    FaceController.Instance.ResetToDefaultFace();
  }

  private void CallPlayFaceAnimation(int faceAnimationIndex) {
    FaceController.Instance.PlayFaceAnimation(faceAnimationIndex);
  }
}