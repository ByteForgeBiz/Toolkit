using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("97F5222E-9379-4C24-9E50-E93C7334BBD5")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class ComparisonDifference
{
	private readonly string _localPath;

	private readonly string _remotePath;

	public SynchronizationAction Action { get; internal set; }

	public bool IsDirectory { get; internal set; }

	public ComparisonFileInfo Local { get; internal set; }

	public ComparisonFileInfo Remote { get; internal set; }

	internal ComparisonDifference(string localPath, string remotePath)
	{
		_localPath = localPath;
		_remotePath = remotePath;
	}

	public override string ToString()
	{
		switch (Action)
		{
		case SynchronizationAction.UploadNew:
		{
			string text = RemotePath.Combine(TranslateLocalPathToRemote(), "*");
			return GetLocalPathString() + " ==> " + text;
		}
		case SynchronizationAction.DownloadNew:
		{
			string text = Path.Combine(TranslateRemotePathToLocal(), "*");
			return text + " <== " + GetRemotePathString();
		}
		case SynchronizationAction.UploadUpdate:
			return GetLocalPathString() + " ==> " + GetRemotePathString();
		case SynchronizationAction.DownloadUpdate:
			return GetLocalPathString() + " <== " + GetRemotePathString();
		case SynchronizationAction.DeleteRemote:
			return "× " + GetRemotePathString();
		case SynchronizationAction.DeleteLocal:
			return "× " + GetLocalPathString();
		default:
			throw new InvalidOperationException();
		}
	}

	private string TranslateRemotePathToLocal()
	{
		return RemotePath.TranslateRemotePathToLocal(RemotePath.GetDirectoryName(Remote.FileName), _remotePath, _localPath);
	}

	private string TranslateLocalPathToRemote()
	{
		return RemotePath.TranslateLocalPathToRemote(Path.GetDirectoryName(Local.FileName), _localPath, _remotePath);
	}

	private string GetRemotePathString()
	{
		return Remote.FileName + (IsDirectory ? "/" : string.Empty);
	}

	private string GetLocalPathString()
	{
		return Local.FileName + (IsDirectory ? "\\" : string.Empty);
	}

	public FileOperationEventArgs Resolve(Session session, TransferOptions options = null)
	{
		if (session == null)
		{
			throw new ArgumentNullException("session");
		}
		switch (Action)
		{
		case SynchronizationAction.UploadNew:
		case SynchronizationAction.UploadUpdate:
		{
			string remoteDirectory = TranslateLocalPathToRemote();
			if (!IsDirectory)
			{
				return session.PutFileToDirectory(Local.FileName, remoteDirectory, remove: false, options);
			}
			session.PutEntryToDirectory(Local.FileName, remoteDirectory, remove: false, options);
			return null;
		}
		case SynchronizationAction.DownloadNew:
		case SynchronizationAction.DownloadUpdate:
		{
			string localDirectory = TranslateRemotePathToLocal();
			if (!IsDirectory)
			{
				return session.GetFileToDirectory(Remote.FileName, localDirectory, remove: false, options);
			}
			session.GetEntryToDirectory(Remote.FileName, localDirectory, remove: false, options);
			return null;
		}
		case SynchronizationAction.DeleteRemote:
			if (!IsDirectory)
			{
				return session.RemoveFile(Remote.FileName);
			}
			session.RemoveEntry(Remote.FileName);
			return null;
		case SynchronizationAction.DeleteLocal:
			if (!IsDirectory)
			{
				File.Delete(Local.FileName);
			}
			else
			{
				Directory.Delete(Local.FileName, recursive: true);
			}
			return null;
		default:
			throw session.Logger.WriteException(new InvalidOperationException());
		}
	}

	public void Reverse()
	{
		switch (Action)
		{
		case SynchronizationAction.UploadNew:
			Action = SynchronizationAction.DeleteLocal;
			break;
		case SynchronizationAction.DownloadNew:
			Action = SynchronizationAction.DeleteRemote;
			break;
		case SynchronizationAction.UploadUpdate:
			Action = SynchronizationAction.DownloadUpdate;
			break;
		case SynchronizationAction.DownloadUpdate:
			Action = SynchronizationAction.UploadUpdate;
			break;
		case SynchronizationAction.DeleteRemote:
			Action = SynchronizationAction.DownloadNew;
			break;
		case SynchronizationAction.DeleteLocal:
			Action = SynchronizationAction.UploadNew;
			break;
		default:
			throw new InvalidOperationException();
		}
	}
}
