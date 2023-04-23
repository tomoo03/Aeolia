using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceController : MonoBehaviour
{
    // Singleton instance
    public static FaceController Instance { get; private set; }

    Animator animator;

    // 表情のアニメを入れる
    public AnimationClip[] animationClips;

    private int faceAnimationCount = 0;

    void Start() {
        // Set singleton instance
        if (Instance == null) {
            Instance = this;
        }
        else if (Instance != this) {
            Destroy(gameObject);
        }

        // Animatorコンポーネントを取得
        animator = GetComponent<Animator>();
        Debug.Log("animator: ");
        Debug.Log(animator);
        Debug.Log(animationClips.Length);
    }

    void Update() {
        // レイヤーの番号、どれだけアニメを出すか（0～１）
        animator.SetLayerWeight(1, 1f);
        // デバッグ用
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("now");
            // PlayFaceAnimation(faceAnimationCount);
            // faceAnimationCount++;
            // if (faceAnimationCount >= animationClips.Length) {
            //     faceAnimationCount = 0;
            // }
        }
    }

    public void PlayFaceAnimation(int faceAnimationIndex) {
        // .nameでアニメーションの名前を取得する
        animator.CrossFade(animationClips[faceAnimationIndex].name, 0);
    }

    public void ResetToDefaultFace() {
        PlayFaceAnimation(FaceAnimationIndex.DEFAULT);
    }
}