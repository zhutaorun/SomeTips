#if UNITY_ANDROID
using UnityEngine;
using System.Collections;

public static class AndroidAssetManager
{
    private static bool _initialized = false;

    private static AndroidJavaObject assetManager;
    private static System.IntPtr methodId_read;

    private static void intialize()
    {
        if (_initialized) return;
        var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");;
        assetManager = activity.Call<AndroidJavaObject>("getAssets");
        _initialized = true;
    }

    public static AndroidAssetStream open(string filePath)
    {
        if (!_initialized) intialize();
        var stream = assetManager.Call<AndroidJavaObject>("open", filePath);
        return new AndroidAssetStream(stream);
    }
}

public class AndroidAssetStream : System.IDisposable
{
    private static readonly System.IntPtr METHOD_read;
    private AndroidJavaObject _stream;
    private int _fileSize;

    public AndroidAssetStream(AndroidJavaObject obj)
    {
        _stream = obj;
        _fileSize = -1;
    }

    public int getFileSize()
    {
        if (_fileSize < 0) _fileSize = _stream.Call<int>("available");
        return _fileSize;
    }

    public byte[] readAllBytes()
    {
        var availbleBytes = getFileSize();
        var byteArray = AndroidJNI.NewByteArray(availbleBytes);
        int readCout = AndroidJNI.CallIntMethod(_stream.GetRawObject(), METHOD_read, new[] { new jvalue() { l = byteArray } });
        var bytes = AndroidJNI.FromByteArray(byteArray);
        AndroidJNI.DeleteGlobalRef(byteArray);
        return bytes;
    }

    public void Dispose()
    {
        if (_stream == null) return;
        var s = _stream;
        _stream = null;

        s.Call("close");
        s.Dispose();
    }

    static AndroidAssetStream()
    {
        var clsPtr = AndroidJNI.FindClass("java.io.InputStream");
        METHOD_read = AndroidJNIHelper.GetMethodID(clsPtr, "read", "([B)I");
    }
}
#endif
