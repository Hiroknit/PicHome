Imports System.IO
Imports System.Text.RegularExpressions

Friend Class ArrangeExecutor

    Private psSourcePath As String = String.Empty
    Private psDestJpgPath As String = String.Empty
    Private psDestRawPath As String = String.Empty
    Private pbIsDateFilterEnabled As Boolean = False
    Private pdtFilterDate As Date = Date.MinValue

    Private psSourceFilePath As String = String.Empty
    Private psDestFilePath As String = String.Empty
    Private psErrorMessage As String = String.Empty
    Private poPhotoFileList As New List(Of String)
    Private poSuccessList As New List(Of String)
    Private poSkipList As New List(Of String)
    Private poFailureList As New List(Of String)

    Private poStopWatch As New Stopwatch()

    Friend ReadOnly Property CopyMessage As String
        Get
            Return $"{psSourceFilePath} ⇒ {psDestFilePath}"
        End Get
    End Property

    Friend ReadOnly Property ErrorMessage As String
        Get
            Return psErrorMessage
        End Get
    End Property

    Friend ReadOnly Property PhotoFileList As List(Of String)
        Get
            Return poPhotoFileList
        End Get
    End Property

    Friend ReadOnly Property FailureList As List(Of String)
        Get
            Return poFailureList
        End Get
    End Property

    Friend ReadOnly Property IsFailedItem As Boolean
        Get
            Return poFailureList.Count <> 0
        End Get
    End Property

    Friend ReadOnly Property ProcessRate As Integer
        Get
            Return ((poSuccessList.Count + poSkipList.Count + poFailureList.Count) / poPhotoFileList.Count) * 100
        End Get
    End Property

    Friend ReadOnly Property DisplayProcessRate As String
        Get
            Return $"{(poSuccessList.Count + poSkipList.Count + poFailureList.Count)}/{poPhotoFileList.Count}"
        End Get
    End Property

    Friend ReadOnly Property ProcessResult As String
        Get
            Me.StopTimer()

            If poStopWatch.Elapsed.TotalMilliseconds < 1000 Then
                Return String.Format("TotalTime：{0} ms (Success：{1}  Skip：{2}  Failure：{3})", poStopWatch.Elapsed.Milliseconds,
                                                                                                  poSuccessList.Count,
                                                                                                  poSkipList.Count,
                                                                                                  poFailureList.Count)

            ElseIf 1 <= poStopWatch.Elapsed.TotalSeconds AndAlso poStopWatch.Elapsed.TotalSeconds < 60 Then
                Return String.Format("TotalTime：{0} s (Success：{1}  Skip：{2}  Failure：{3})", poStopWatch.Elapsed.Seconds,
                                                                                                 poSuccessList.Count,
                                                                                                 poSkipList.Count,
                                                                                                 poFailureList.Count)

            ElseIf 1 <= poStopWatch.Elapsed.TotalMinutes AndAlso poStopWatch.Elapsed.TotalMinutes < 60 Then
                Return String.Format("TotalTime：{0} m (Success：{1}  Skip：{2}  Failure：{3})", poStopWatch.Elapsed.Minutes,
                                                                                                 poSuccessList.Count,
                                                                                                 poSkipList.Count,
                                                                                                 poFailureList.Count)

            Else
                Return String.Format("TotalTime：{0} h (Success：{1}  Skip：{2}  Failure：{3})", poStopWatch.Elapsed.TotalHours.ToString("F1"),
                                                                                                 poSuccessList.Count,
                                                                                                 poSkipList.Count,
                                                                                                 poFailureList.Count)

            End If
        End Get
    End Property

    Friend Sub New(ByVal asSourcePath As String, asDestJpgPath As String, asDestRawPath As String, ByVal abIsJpgEnabled As Boolean, ByVal abIsRawEnabled As Boolean)
        Me.New(asSourcePath, asDestJpgPath, asDestRawPath, abIsJpgEnabled, abIsRawEnabled, False, DateTime.Now)
    End Sub

    Friend Sub New(ByVal asSourcePath As String, asDestJpgPath As String, asDestRawPath As String, ByVal abIsJpgEnabled As Boolean, ByVal abIsRawEnabled As Boolean, ByVal abIsDateFilterEnabled As Boolean, ByVal adtFilterDateTime As DateTime)
        psSourcePath = asSourcePath
        psDestJpgPath = asDestJpgPath
        psDestRawPath = asDestRawPath
        pbIsDateFilterEnabled = abIsDateFilterEnabled
        pdtFilterDate = adtFilterDateTime

        ' ファイルタイプの有効/無効に基づいてフォルダ名を調整
        ' JPGが無効の場合、JPGのフォルダは使わないので、RAWフォルダを代わりに指定する必要はない
        ' 各ファイルが処理時に適切なパスに送られるため、ここでは何もしない

        poPhotoFileList = Me.GetPhotoFile(abIsJpgEnabled, abIsRawEnabled)

        If poPhotoFileList.Count <> 0 Then
            poStopWatch.Start()
        End If

    End Sub

    Private Function GetPhotoFile(ByVal abIsJpgEnabled As Boolean, ByVal abIsRawEnabled As Boolean) As List(Of String)
        Dim patterns As String() = Me.GetPatterns(abIsJpgEnabled, abIsRawEnabled)
        Dim loPhotoFiles = (From file In New DirectoryInfo(psSourcePath).GetFiles("*.*", SearchOption.AllDirectories)
                            Where patterns.Contains(file.Extension.ToUpper())
                            Select file)
        
        ' 日時フィルターが有効な場合、指定日時以降のファイルのみを返す
        If pbIsDateFilterEnabled Then
            Return (From file In loPhotoFiles
                    Where file.LastWriteTime >= pdtFilterDate
                    Select file.FullName).ToList()
        Else
            Return (From file In loPhotoFiles
                    Select file.FullName).ToList()
        End If
    End Function

    Private Function GetPatterns(ByVal abIsJpgEnabled As Boolean, ByVal abIsRawEnabled As Boolean) As String()
        Dim patternsList As New List(Of String)
        
        If abIsJpgEnabled Then
            patternsList.Add(".JPG")
        End If
        
        If abIsRawEnabled Then
            patternsList.Add(".ARW")
        End If
        
        Return patternsList.ToArray()
    End Function

    Friend Sub StopTimer()

        If poStopWatch.IsRunning Then poStopWatch.Stop()

    End Sub


    Friend Function Execute(ByVal asSourceFilePath As String) As ProcessStatus
        Try
            Dim loSourceFileInfo As New FileInfo(asSourceFilePath)
            Dim lsFileDate As String = loSourceFileInfo.LastWriteTime.ToString("yyyyMMdd")

            Dim lsNewFolderPath As String = String.Empty
            Select Case loSourceFileInfo.Extension.ToUpper()
                Case ".JPG" : lsNewFolderPath = Path.Combine(psDestJpgPath, lsFileDate)
                Case ".ARW" : lsNewFolderPath = Path.Combine(psDestRawPath, lsFileDate)
            End Select

            ' Directory.CreateDirectory() は既存フォルダなら何もしないため、存在チェック不要
            Directory.CreateDirectory(lsNewFolderPath)

            psSourceFilePath = asSourceFilePath
            psDestFilePath = Path.Combine(lsNewFolderPath, loSourceFileInfo.Name)

            If File.Exists(psDestFilePath) Then
                poSkipList.Add(asSourceFilePath)
                Return ProcessStatus.Skipped
            End If

            'コピー後のファイルの更新日付をコピー元と同じにするため、File.Copy() を使用してから更新日時を設定する方法を選択
            '' FileStream を使用してバッファサイズ(64KB)を指定
            'Using sourceStream = New FileStream(asSourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536)
            '    Using destStream = New FileStream(psDestFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536)
            '        sourceStream.CopyTo(destStream, 65536)
            '    End Using
            'End Using

            File.Copy(asSourceFilePath, psDestFilePath)

            poSuccessList.Add(asSourceFilePath)
            Return ProcessStatus.Success

        Catch ex As Exception
            psErrorMessage = ex.Message
            poFailureList.Add(asSourceFilePath)
            Return ProcessStatus.Failure
        End Try

    End Function

End Class
