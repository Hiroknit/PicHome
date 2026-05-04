Imports System.Runtime.InteropServices
Imports System.ComponentModel

Friend Class FileServerConnector

    Private psServerPath As String = String.Empty
    Private psUserName As String = String.Empty
    Private psPassword As String = String.Empty

    Private pbIsConnected As Boolean = False

    Private Shared poInstance As FileServerConnector = Nothing

    Friend Shared Function GetInstance(ByVal asFileServerPath As String) As FileServerConnector

        If poInstance Is Nothing Then
            poInstance = New FileServerConnector()
        End If

        Return poInstance

    End Function

    Private Sub New()
        psUserName = My.Settings.UserName
        psPassword = My.Settings.Password
    End Sub

    ''' <summary>
    ''' 非同期で接続を確立する
    ''' </summary>
    Friend Async Function Connect(ByVal asServerPath As String) As Task(Of Boolean)

        Dim nr As New NETRESOURCE With {
            .dwType = 1, ' RESOURCETYPE_DISK
            .lpRemoteName = asServerPath
        }

        Dim liResult As Integer = Await Task.Run(Function() WNetAddConnection2(nr, psPassword, psUserName, 0))

        If liResult <> 0 Then Throw New Win32Exception(liResult) ' 呼び出し元でエラー詳細を把握させる

        psServerPath = asServerPath
        pbIsConnected = True

        Return True

    End Function

    ''' <summary>
    ''' 非同期で切断する
    ''' </summary>
    Public Async Function Disconnect(Optional force As Boolean = True) As Task(Of Boolean)

        If Not pbIsConnected Then Return True

        Dim liResult As Integer = Await Task.Run(Function() WNetCancelConnection2(psServerPath, 0, force))

        If liResult <> 0 Then Return False

        pbIsConnected = False

        Return True

    End Function

    ' --- API定義 ---
    <StructLayout(LayoutKind.Sequential)>
    Public Structure NETRESOURCE
        Public dwType As Integer
        Public lpLocalName As String
        Public lpRemoteName As String
        Public lpProvider As String
    End Structure

    <DllImport("mpr.dll", CharSet:=CharSet.Auto)>
    Private Shared Function WNetAddConnection2(ByRef lpNetResource As NETRESOURCE, ByVal lpPassword As String, ByVal lpUserName As String, ByVal dwFlags As Integer) As Integer
    End Function

    <DllImport("mpr.dll", CharSet:=CharSet.Auto)>
    Private Shared Function WNetCancelConnection2(ByVal lpName As String, ByVal dwFlags As Integer, ByVal fForce As Boolean) As Integer
    End Function

End Class
