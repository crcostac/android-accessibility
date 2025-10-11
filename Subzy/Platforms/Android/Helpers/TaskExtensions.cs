using Android.Gms.Extensions;
using Android.Runtime;

namespace Subzy.Platforms.Android.Helpers;

/// <summary>
/// Extension methods for converting Google Play Services Task to .NET Task.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Converts a Google Play Services Task to a .NET Task.
    /// </summary>
    public static Task<TResult> AsAsync<TResult>(this global::Android.Gms.Tasks.Task task) where TResult : Java.Lang.Object
    {
        var tcs = new TaskCompletionSource<TResult>();

        task.AddOnSuccessListener(new OnSuccessListener<TResult>(result =>
        {
            tcs.SetResult(result);
        }));

        task.AddOnFailureListener(new OnFailureListener(exception =>
        {
            tcs.SetException(new Exception(exception.Message, exception));
        }));

        return tcs.Task;
    }

    private class OnSuccessListener<TResult> : Java.Lang.Object, global::Android.Gms.Tasks.IOnSuccessListener where TResult : Java.Lang.Object
    {
        private readonly Action<TResult> _onSuccess;

        public OnSuccessListener(Action<TResult> onSuccess)
        {
            _onSuccess = onSuccess;
        }

        public void OnSuccess(Java.Lang.Object? result)
        {
            if (result is TResult typedResult)
            {
                _onSuccess(typedResult);
            }
        }
    }

    private class OnFailureListener : Java.Lang.Object, global::Android.Gms.Tasks.IOnFailureListener
    {
        private readonly Action<Java.Lang.Exception> _onFailure;

        public OnFailureListener(Action<Java.Lang.Exception> onFailure)
        {
            _onFailure = onFailure;
        }

        public void OnFailure(Java.Lang.Exception exception)
        {
            _onFailure(exception);
        }
    }
}
