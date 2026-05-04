Imports System.IO
Imports System.ComponentModel
Imports System.Windows.Forms

Namespace AutoArrangePhotoFolder

    Partial Public Class MainWindow

        Private poExecutor As ArrangeExecutor = Nothing
        Private poBackgroundWorker As New BackgroundWorker()

        Public Sub New()

            InitializeComponent()

            ' BackgroundWorkerの設定
            AddHandler poBackgroundWorker.DoWork, AddressOf BackgroundWorker_DoWork
            AddHandler poBackgroundWorker.ProgressChanged, AddressOf BackgroundWorker_ProgressChanged
            AddHandler poBackgroundWorker.RunWorkerCompleted, AddressOf BackgroundWorker_RunWorkerCompleted
            poBackgroundWorker.WorkerReportsProgress = True
            poBackgroundWorker.WorkerSupportsCancellation = True

            ' Loadedイベントハンドラを追加
            AddHandler Me.Loaded, AddressOf Window_Loaded

        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            Me.LoadSettings()
            Me.EnableControl(True)
            Me.RefreshControl()
            Me.ClearSetting()
        End Sub

        Private Sub BackgroundWorker_DoWork(sender As Object, e As DoWorkEventArgs)

            If poExecutor.PhotoFileList.Count = 0 Then
                poBackgroundWorker.ReportProgress(0, Me.FormatMessage("コピー対象のファイルが存在しません。"))
                Return
            End If

            poBackgroundWorker.ReportProgress(0, "-----Copy Start----------------------------------------------------------" & Environment.NewLine)

            For Each lsPhotoFile As String In poExecutor.PhotoFileList

                If poBackgroundWorker.CancellationPending Then
                    poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, "-----Copy Canceled---------------------------------------------------------")
                    poExecutor.StopTimer()
                    e.Cancel = True
                    Return
                End If

                Select Case poExecutor.Execute(lsPhotoFile)

                    Case ProcessStatus.Success
                        poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, Me.FormatMessage($"Success ({poExecutor.DisplayProcessRate})：{poExecutor.CopyMessage}"))

                    Case ProcessStatus.Skipped
                        poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, Me.FormatMessage($"Skipped ({poExecutor.DisplayProcessRate})：{poExecutor.CopyMessage}"))

                    Case ProcessStatus.Failure
                        poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, Me.FormatMessage($"Failure ({poExecutor.DisplayProcessRate})：{poExecutor.CopyMessage}"))
                        poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, poExecutor.ErrorMessage)

                End Select
            Next

            poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, "-----Copy Finished----------------------------------------------------------")

            poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, poExecutor.ProcessResult)

            poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, "----------------------------------------------------------------------------")

            If poExecutor.IsFailedItem Then

                poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, "-----Failure List--------------------------------------------------------------")

                For Each lsFailedPath As String In poExecutor.FailureList
                    poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, $"- {lsFailedPath}")
                Next

                poBackgroundWorker.ReportProgress(poExecutor.ProcessRate, "------------------------------------------------------------------------------")

            End If

        End Sub

        Private Sub BackgroundWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs)

            Me.richtxtMessage.AppendText(e.UserState.ToString() & Environment.NewLine)

            'If piCount Mod 5 = 1 Then
            '    Me.richtxtMessage.ScrollToEnd()
            'End If

            Me.richtxtMessage.ScrollToEnd()

            Me.ProgressBar.Value = e.ProgressPercentage
            Me.ProgressLabel.Text = String.Format("{0}%", e.ProgressPercentage)

        End Sub

        Private Sub BackgroundWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)

            Me.EnableControl(True)

        End Sub

        Private Sub Execute()

            If Not Me.btnExecute.IsEnabled Then Return

            Dim loResult = Windows.MessageBox.Show("写真整理を実行しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question)
            If loResult <> MessageBoxResult.Yes Then
                Return
            End If

            Me.RefreshControl()

            If Not Directory.Exists(Me.txtSourcePath.Text) Then
                Me.richtxtMessage.AppendText("処理対象のフォルダが存在しません。")
                Return
            End If

            ' JPG、RAWとも無効の場合はエラーメッセージを表示
            If Not CBool(Me.cbJpgEnabled.IsChecked) AndAlso Not CBool(Me.cbRawEnabled.IsChecked) Then
                Me.richtxtMessage.AppendText("JPG または RAW のいずれかを有効にしてください。")
                Return
            End If

            ' 日時フィルター情報を取得
            Dim lbIsFilterEnabled As Boolean = CBool(Me.cbDateFilterEnabled.IsChecked)
            Dim loFilterDateTime As Date = Date.MinValue

            If lbIsFilterEnabled Then
                ' 日付と時刻を組み合わせてDateTime を作成
                If Not Me.dpFilterDate.SelectedDate.HasValue Then
                    Me.richtxtMessage.AppendText("フィルター日付を選択してください。")
                    Return
                End If

                loFilterDateTime = New Date(Me.dpFilterDate.SelectedDate.Value.Year, Me.dpFilterDate.SelectedDate.Value.Month, Me.dpFilterDate.SelectedDate.Value.Day)
            End If

            poExecutor = New ArrangeExecutor(Me.txtSourcePath.Text, Me.txtDestJpgPath.Text, Me.txtDestRawPath.Text, 
                                            CBool(Me.cbJpgEnabled.IsChecked), CBool(Me.cbRawEnabled.IsChecked),
                                            lbIsFilterEnabled, loFilterDateTime)

            Me.EnableControl(False)

            poBackgroundWorker.RunWorkerAsync()

        End Sub

        Private Sub Cancel()

            If Not Me.btnCancel.IsEnabled Then Return

            If poBackgroundWorker.IsBusy Then
                Dim loResult = System.Windows.MessageBox.Show("中断しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question)
                If loResult = MessageBoxResult.Yes Then
                    poBackgroundWorker.CancelAsync()
                End If
            End If

        End Sub

        Private Sub CloseForm()

            Me.Close()

        End Sub

        Private Sub EnableControl(ByVal abIsEnable As Boolean)

            Me.btnExecute.IsEnabled = abIsEnable
            Me.btnCancel.IsEnabled = Not abIsEnable
            Me.txtSourcePath.IsEnabled = abIsEnable
            Me.btnBrowse1.IsEnabled = abIsEnable
            Me.cbJpgEnabled.IsEnabled = abIsEnable
            Me.cbRawEnabled.IsEnabled = abIsEnable
            Me.UpdateFieldState()

        End Sub

        Private Sub RefreshControl()

            Me.richtxtMessage.Document.Blocks.Clear()
            Me.ProgressBar.Value = 0

        End Sub

        Private Sub ClearSetting()

            ' 設定をリセット（初期状態に戻す）
            Me.RefreshControl()

        End Sub

        Private Function BrowseFolder(ByVal asFolderPath As String) As String

            Dim lsDirectory As String = String.Empty

            Using loOFD As New OpenFileDialog()
                loOFD.FileName = "SelectFolder"
                loOFD.Filter = "Folder|."
                loOFD.InitialDirectory = If(Directory.Exists(asFolderPath), asFolderPath, "")
                loOFD.CheckFileExists = False

                If loOFD.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
                    lsDirectory = Path.GetDirectoryName(loOFD.FileName)
                End If
            End Using

            If lsDirectory = String.Empty Then Return asFolderPath

            Return lsDirectory

        End Function

        Private Function FormatMessage(ByVal asMessage As String) As String
            Return String.Format("{0}{1}{2}", Date.Now.ToString("HH:mm:ss.fff"), vbTab, asMessage)
        End Function

        Private Sub LoadSettings()

            Me.txtSourcePath.Text = My.Settings.SourcePath
            Me.txtDestJpgPath.Text = My.Settings.DestJpgPath
            Me.txtDestRawPath.Text = My.Settings.DestRawPath
            Me.cbJpgEnabled.IsChecked = My.Settings.IsJpgEnabled
            Me.cbRawEnabled.IsChecked = My.Settings.IsRawEnabled
            Me.cbDateFilterEnabled.IsChecked = My.Settings.IsDateFilterEnabled

            If Date.TryParse(My.Settings.FilterDate, Nothing) Then
                Me.dpFilterDate.SelectedDate = Date.Parse(My.Settings.FilterDate)
            Else
                Me.dpFilterDate.SelectedDate = Date.Now
            End If

            ' 初期時にチェックボックスの状態に応じてフィールドを有効/無効にする
            Me.UpdateFieldState()

        End Sub

        Private Sub SaveSettings()

            My.Settings.SourcePath = Me.txtSourcePath.Text
            My.Settings.DestJpgPath = Me.txtDestJpgPath.Text
            My.Settings.DestRawPath = Me.txtDestRawPath.Text
            My.Settings.IsJpgEnabled = Me.cbJpgEnabled.IsChecked
            My.Settings.IsRawEnabled = Me.cbRawEnabled.IsChecked
            My.Settings.IsDateFilterEnabled = Me.cbDateFilterEnabled.IsChecked
            My.Settings.FilterDate = Me.dpFilterDate.SelectedDate.Value
            My.Settings.Save()

        End Sub

        Private Sub btnBrowse1_Click(sender As Object, e As RoutedEventArgs)
            Me.txtSourcePath.Text = Me.BrowseFolder(Me.txtSourcePath.Text)
        End Sub

        Private Sub btnBrowseJpg_Click(sender As Object, e As RoutedEventArgs)
            Me.txtDestJpgPath.Text = Me.BrowseFolder(Me.txtDestJpgPath.Text)
        End Sub

        Private Sub btnBrowseRaw_Click(sender As Object, e As RoutedEventArgs)
            Me.txtDestRawPath.Text = Me.BrowseFolder(Me.txtDestRawPath.Text)
        End Sub

        Private Sub cbJpgEnabled_Checked(sender As Object, e As RoutedEventArgs)
            Me.UpdateFieldState()
        End Sub

        Private Sub cbJpgEnabled_Unchecked(sender As Object, e As RoutedEventArgs)
            Me.UpdateFieldState()
        End Sub

        Private Sub cbRawEnabled_Checked(sender As Object, e As RoutedEventArgs)
            Me.UpdateFieldState()
        End Sub

        Private Sub cbRawEnabled_Unchecked(sender As Object, e As RoutedEventArgs)
            Me.UpdateFieldState()
        End Sub

        Private Sub cbDateFilterEnabled_Checked(sender As Object, e As RoutedEventArgs)
            Me.UpdateFieldState()
        End Sub

        Private Sub cbDateFilterEnabled_Unchecked(sender As Object, e As RoutedEventArgs)
            Me.UpdateFieldState()
        End Sub

        Private Sub UpdateFieldState()
            ' JPG有効状態に応じてJPGテキストボックスとボタンを有効/無効に
            Me.txtDestJpgPath.IsEnabled = CBool(Me.cbJpgEnabled.IsChecked)
            Me.btnBrowseJpg.IsEnabled = CBool(Me.cbJpgEnabled.IsChecked)

            ' RAW有効状態に応じてRAWテキストボックスとボタンを有効/無効に
            Me.txtDestRawPath.IsEnabled = CBool(Me.cbRawEnabled.IsChecked)
            Me.btnBrowseRaw.IsEnabled = CBool(Me.cbRawEnabled.IsChecked)

            ' 日時フィルター有効状態に応じて日時ピッカーを有効/無効に
            Me.dpFilterDate.IsEnabled = CBool(Me.cbDateFilterEnabled.IsChecked)
        End Sub

        Private Sub btnCancel_Click(sender As Object, e As RoutedEventArgs)
            Me.Cancel()
        End Sub

        Private Sub btnExecute_Click(sender As Object, e As RoutedEventArgs)
            Me.Execute()
        End Sub

        Private Sub Window_Closing(sender As Object, e As CancelEventArgs)

            If poBackgroundWorker.IsBusy Then
                Dim loResult = Windows.MessageBox.Show("コピー処理中ですが、アプリケーションを終了しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question)
                If loResult <> MessageBoxResult.Yes Then
                    e.Cancel = True
                    Return
                End If
            End If

            Me.SaveSettings()

            poBackgroundWorker.CancelAsync()

            Dim loStopwatch As New Stopwatch()
            loStopwatch.Start()

            While (True)

                'BackgroundWorkerが終了するのを待機
                If Not poBackgroundWorker.IsBusy Then
                    Exit While
                End If

                If loStopwatch.Elapsed.TotalSeconds > 10 Then
                    ' 10秒以上経過してもBackgroundWorkerが終了しない場合は強制終了
                    Exit While
                End If

                System.Threading.Thread.Sleep(500)

            End While

            loStopwatch.Stop()

            poExecutor = Nothing

        End Sub

        Private Sub Window_PreviewKeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs)
            Select Case e.Key
                Case Key.F2
                    Me.CloseForm()
                    e.Handled = True

                Case Key.Escape
                    Me.Cancel()
                    e.Handled = True

                Case Key.F12
                    Me.Execute()
                    e.Handled = True

                Case Key.Home
                    ' Shift+Homeをチェック
                    If (Keyboard.Modifiers And ModifierKeys.Shift) = ModifierKeys.Shift Then
                        Me.RefreshControl()
                        Me.ClearSetting()
                        e.Handled = True
                    End If
            End Select
        End Sub

    End Class

End Namespace
