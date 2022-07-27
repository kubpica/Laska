using Google.Play.Review;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class InappReview : MonoBehaviourSingleton<InappReview>
{
    public UnityEvent onRequestFailed;
    public UnityEvent onLaunchFailed;
    public UnityEvent onLaunchFinished;

    private ReviewManager _reviewManager;
    private PlayReviewInfo _playReviewInfo;
    private Coroutine _requestCoroutine;
    private Coroutine _launchCoroutine;

    public string Error { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        _reviewManager = new ReviewManager();
    }

    public void RequestReview()
    {
        _requestCoroutine = StartCoroutine(requestReviewInfo());
    }

    private IEnumerator requestReviewInfo()
    {
        var requestFlowOperation = _reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;
        _requestCoroutine = null;
        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Error = requestFlowOperation.Error.ToString();
            Debug.LogError(Error);
            onRequestFailed.Invoke();
            yield break;
        }
        _playReviewInfo = requestFlowOperation.GetResult();
    } 

    public void LaunchReview()
    {
        _launchCoroutine = StartCoroutine(launchReview());
        StartCoroutine(timeoutLaunch());
    }

    private IEnumerator timeoutLaunch()
    {
        float timer = 0;
        while (_launchCoroutine != null)
        {
            if (timer > 3)
            {
                StopCoroutine(_launchCoroutine);
                if (_requestCoroutine != null)
                    StopCoroutine(_requestCoroutine);
                onLaunchFailed.Invoke();
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator launchReview()
    {
        while (_requestCoroutine != null)
        {
            yield return null;
        }

        Error = null;
        while (_playReviewInfo == null && Error == null)
        {
            yield return requestReviewInfo();
        }
        if (Error != null)
        {
            failed();
            yield break;
        }

        var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
        yield return launchFlowOperation;
        _playReviewInfo = null; // Reset the object
        _launchCoroutine = null;
        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Error = launchFlowOperation.Error.ToString();
            failed();
            yield break;
        }
        // The flow has finished. The API does not indicate whether the user
        // reviewed or not, or even whether the review dialog was shown. Thus, no
        // matter the result, we continue our app flow
        onLaunchFinished.Invoke();

        void failed()
        {
            Debug.LogError(Error);
            onLaunchFailed.Invoke();
        }
    }
}
