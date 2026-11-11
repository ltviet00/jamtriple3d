using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

///<summary> Working with UniTask, we alway have to declare CancellationToken object which cost the same bunch of code.
///Let just wrap them to here</summary>
public class UniTaskTokenHelper {
	CancellationTokenSource _cancelTokenSource;
	public CancellationToken Token {
		get {
			if(_cancelTokenSource == null) {
				_cancelTokenSource = new CancellationTokenSource();
			}
			return _cancelTokenSource.Token;
		}
	}

	public void ClearRunningTasks() {
		_cancelTokenSource?.Cancel();
		_cancelTokenSource?.Dispose();
		_cancelTokenSource = null;
	}
}
