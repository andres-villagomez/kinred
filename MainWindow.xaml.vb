Imports Microsoft.Speech.Recognition
Imports Microsoft.Speech.AudioFormat
Imports Microsoft.Kinect
Imports System.Windows.Threading
Imports System.Threading
Imports System.IO
Imports Microsoft.Speech.Synthesis

Namespace KinectAudioDemo
	Partial Public Class MainWindow
		Inherits Window
        Private Const AcceptedSpeechPrefix As String = ""
        Private Const RejectedSpeechPrefix As String = ""

		Private Const WaveImageWidth As Integer = 500
		Private Const WaveImageHeight As Integer = 100

        Private ReadOnly EchoWave As WriteableBitmap
		Private ReadOnly pixels() As Byte
		Private ReadOnly energyBuffer(WaveImageWidth - 1) As Double
		Private ReadOnly blackPixels(WaveImageWidth * WaveImageHeight - 1) As Byte
		Private ReadOnly fullImageRect As New Int32Rect(0, 0, WaveImageWidth, WaveImageHeight)

		Private kinect As KinectSensor
		Private angle As Double
		Private running As Boolean = True
		Private readyTimer As DispatcherTimer
		Private stream As EnergyCalculatingPassThroughStream
        Private speechRecognizer As SpeechRecognitionEngine
        Dim speak As SpeechSynthesizer
		Public Sub New()
			InitializeComponent()
            Dim colorList = New List(Of Color) From {Colors.White, Colors.Brown}
            Me.EchoWave = New WriteableBitmap(WaveImageWidth, WaveImageHeight, 96, 96, PixelFormats.Indexed1, New BitmapPalette(colorList))
            Me.pixels = New Byte(WaveImageWidth - 1) {}
			For i As Integer = 0 To Me.pixels.Length - 1
				Me.pixels(i) = &Hff
			Next i
            imgWav.Source = Me.EchoWave
			AddHandler SensorChooser.KinectSensorChanged, AddressOf SensorChooserKinectSensorChanged
		End Sub

		Private Shared Function GetKinectRecognizer() As RecognizerInfo
			Dim matchingFunc As Func(Of RecognizerInfo, Boolean) = Function(r)
				Dim value As String
				r.AdditionalInfo.TryGetValue("Kinect", value)
				Return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) AndAlso "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase)
			End Function
			Return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault()
		End Function

		Private Sub SensorChooserKinectSensorChanged(ByVal sender As Object, ByVal e As DependencyPropertyChangedEventArgs)
            Dim listen_init As KinectSensor = TryCast(e.NewValue, KinectSensor)
            Me.kinect = listen_init
			enableAec.IsEnabled = Me.kinect IsNot Nothing
            If listen_init IsNot Nothing Then
                Me.InitializeKinect()
            End If
		End Sub

		Private Sub InitializeKinect()
			Dim sensor = Me.kinect
			Me.speechRecognizer = Me.CreateSpeechRecognizer()
			Try
				sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30)
				sensor.Start()
			Catch e1 As Exception
				SensorChooser.AppConflictOccurred()
				Return
			End Try

			If Me.speechRecognizer IsNot Nothing AndAlso sensor IsNot Nothing Then
				' NOTE: Need to wait 4 seconds for device to be ready to stream audio right after initialization
				Me.readyTimer = New DispatcherTimer()
				AddHandler Me.readyTimer.Tick, AddressOf ReadyTimerTick
				Me.readyTimer.Interval = New TimeSpan(0, 0, 4)
				Me.readyTimer.Start()

				Me.ReportSpeechStatus("Initializing audio stream...")
				Me.UpdateInstructionsText(String.Empty)

				AddHandler Me.Closing, AddressOf MainWindowClosing
			End If

			Me.running = True
		End Sub

		Private Sub ReadyTimerTick(ByVal sender As Object, ByVal e As EventArgs)
			Me.Start()
            Me.ReportSpeechStatus("Speak a command")
            Me.UpdateInstructionsText("Real time status")
			Me.readyTimer.Stop()
			Me.readyTimer = Nothing
		End Sub

		Private Sub UninitializeKinect()
			Dim sensor = Me.kinect
			Me.running = False
			If Me.speechRecognizer IsNot Nothing AndAlso sensor IsNot Nothing Then
				sensor.AudioSource.Stop()
				sensor.Stop()
				Me.speechRecognizer.RecognizeAsyncCancel()
				Me.speechRecognizer.RecognizeAsyncStop()
			End If

			If Me.readyTimer IsNot Nothing Then
				Me.readyTimer.Stop()
				Me.readyTimer = Nothing
			End If
		End Sub

		Private Sub MainWindowClosing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs)
			Me.UninitializeKinect()
		End Sub

		Private Function CreateSpeechRecognizer() As SpeechRecognitionEngine
			Dim ri As RecognizerInfo = GetKinectRecognizer()
			Dim sre As SpeechRecognitionEngine
			Try
				sre = New SpeechRecognitionEngine(ri.Id)
			Catch
				MessageBox.Show("There was a problem initializing Speech Recognition." & ControlChars.CrLf & "Ensure you have the Microsoft Speech SDK installed and configured.", "Failed to load Speech SDK", MessageBoxButton.OK, MessageBoxImage.Error)
				Me.Close()
				Return Nothing
			End Try

			Dim grammar = New Choices()
			grammar.Add("Andres Hernandez Camera on")
			grammar.Add("Andres Hernandez Camera off")
			grammar.Add("Andres Hernandez Open Explorer")
			grammar.Add("Andres Hernandez Close Explorer")
			grammar.Add("Andres Hernandez Open Chrome")
			grammar.Add("Andres Hernandez Close Chrome")
			grammar.Add("Andres Hernandez Open Command Prompt")
			grammar.Add("Andres Hernandez Close Command Prompt")
			grammar.Add("Andres Hernandez Open Bloc")
			grammar.Add("Andres Hernandez Close Bloc")
			grammar.Add("Andres Hernandez Open Wire Shark")
			grammar.Add("Andres Hernandez Close Wire Shark")
			grammar.Add("Andres Hernandez Open Python")
			grammar.Add("Andres Hernandez Close Python")
			grammar.Add("Andres Hernandez Open Skype")
			grammar.Add("Andres Hernandez Close Skype")
			grammar.Add("Andres Hernandez Open Itunes")
			grammar.Add("Andres Hernandez Close Itunes")
			grammar.Add("Goodbye Andres Hernandez")
			grammar.Add("Andres Hernandez Visit Google")
			grammar.Add("Andres Hernandez Visit Face Book")
			grammar.Add("Andres Hernandez Visit Hot Mail")
			grammar.Add("Andres Hernandez Visit G Mail")
			grammar.Add("Andres Hernandez Visit You Tube")
			grammar.Add("Andres Hernandez What Is On The News")
			grammar.Add("Andres Hernandez Visit X Box")

			Dim gb = New GrammarBuilder With {.Culture = ri.Culture}
			gb.Append(grammar)
			Dim g = New Grammar(gb)
            sre.LoadGrammar(g)
			AddHandler sre.SpeechRecognized, AddressOf SreSpeechRecognized
			AddHandler sre.SpeechHypothesized, AddressOf SreSpeechHypothesized
			AddHandler sre.SpeechRecognitionRejected, AddressOf SreSpeechRecognitionRejected
			Return sre
		End Function

		Private Sub RejectSpeech(ByVal result As RecognitionResult)
            Dim status As String = "I don´t understand: " & (If(result Is Nothing, String.Empty, result.Text & " " & result.Confidence))
            Me.ReportSpeechStatus(status)
		End Sub

		Private Sub SreSpeechRecognitionRejected(ByVal sender As Object, ByVal e As SpeechRecognitionRejectedEventArgs)
			Me.RejectSpeech(e.Result)
		End Sub

		Private Sub SreSpeechHypothesized(ByVal sender As Object, ByVal e As SpeechHypothesizedEventArgs)
            Me.ReportSpeechStatus("Processing: " & e.Result.Text & " " & e.Result.Confidence)
		End Sub

        Private Sub SreSpeechRecognized(ByVal sender As Object, ByVal e As SpeechRecognizedEventArgs)
            If e.Result.Confidence < 0.5 Then
                Me.RejectSpeech(e.Result)
                Return
            End If

            Select Case e.Result.Text.ToUpperInvariant()
				Case "ANDRES HERNANDEZ CAMERA ON"
					Me.kinectColorViewer1.Visibility = Visibility.Visible
				Case "ANDRES HERNANDEZ CAMERA OFF"
					Me.kinectColorViewer1.Visibility = Visibility.Hidden
				Case "ANDRES HERNANDEZ OPEN EXPLORER"
					Process.Start("iexplore.exe")
				Case "ANDRES HERNANDEZ CLOSE EXPLORER"
					Process.GetProcessesByName("InternetExplorer")(0).Kill()
				Case "ANDRES HERNANDEZ OPEN CHROME"
					Process.Start("chrome.exe")
				Case "ANDRES HERNANDEZ CLOSE CHROME"
					Process.GetProcessesByName("chrome")(0).Kill()
				Case "ANDRES HERNANDEZ OPEN COMMAND PROMPT"
					Process.Start("cmd.exe")
				Case "ANDRES HERNANDEZ CLOSE COMMAND PROMPT"
					Process.GetProcessesByName("cmd")(0).Kill()
				Case "ANDRES HERNANDEZ OPEN BLOC"
					Process.Start("notepad.exe")
				Case "ANDRES HERNANDEZ CLOSE BLOC"
					Process.GetProcessesByName("notepad")(0).Kill()
				Case "ANDRES HERNANDEZ OPEN WIRE SHARK"
					Process.Start("wireshark.exe")
				Case "ANDRES HERNANDEZ CLOSE WIRESHARK"
					Process.GetProcessesByName("wireshark")(0).Kill()
				Case "ANDRES HERNANDEZ OPEN PYTHON"
					Process.Start("python.exe")
				Case "ANDRES HERNANDEZ CLOSE PYTHON"
					Process.GetProcessesByName("python")(0).Kill()
				Case "ANDRES HERNANDEZ OPEN SKYPE"
					Process.Start("skype.exe")
				Case "ANDRES HERNANDEZ CLOSE SKYPE"
					Process.GetProcessesByName("skype")(0).Kill()
				Case "ANDRES HERNANDEZ OPEN ITUNES"
					Process.Start("itunes.exe")
				Case "ANDRES HERNANDEZ CLOSE ITUNES"
					Process.GetProcessesByName("itunes")(0).Kill()
				Case "ANDRES HERNANDEZ VISIT GOOGLE"
					System.Diagnostics.Process.Start("https://www.google.com")
				Case "ANDRES HERNANDEZ VISIT FACE BOOK"
					System.Diagnostics.Process.Start("https://www.facebook.com")
				Case "ANDRES HERNANDEZ VISIT HOT MAIL"
					System.Diagnostics.Process.Start("https://www.hotmail.com")
				Case "ANDRES HERNANDEZ VISIT G MAIL"
					System.Diagnostics.Process.Start("https://www.gmail.com")
				Case "ANDRES HERNANDEZ VISIT YOU TUBE"
					System.Diagnostics.Process.Start("https://www.youtube.com")
				Case "ANDRES HERNANDEZ WHAT IS ON THE NEWS"
					System.Diagnostics.Process.Start("http://www.news.google.com")
				Case "ANDRES HERNANDEZ VISIT X BOX"
					System.Diagnostics.Process.Start("https://www.xbox.com")
				Case "GOODBYE ANDRES HERNANDEZ"
					Me.Close()
                Case Else
                    Me.ShowDialog()
            End Select
            Dim status As String = "Understood: " & e.Result.Text & " " & e.Result.Confidence
            Me.ReportSpeechStatus(status)
        End Sub

		Private Sub ReportSpeechStatus(ByVal status As String)
			Dispatcher.BeginInvoke(New Action(Sub() tbSpeechStatus.Text = status), DispatcherPriority.Normal)
		End Sub

		Private Sub UpdateInstructionsText(ByVal instructions As String)
			Dispatcher.BeginInvoke(New Action(Sub() tbColor.Text = instructions), DispatcherPriority.Normal)
		End Sub

		Private Sub Start()
			Dim audioSource = Me.kinect.AudioSource
			audioSource.BeamAngleMode = BeamAngleMode.Adaptive
			Dim kinectStream = audioSource.Start()
			Me.stream = New EnergyCalculatingPassThroughStream(kinectStream)
			Me.speechRecognizer.SetInputToAudioStream(Me.stream, New SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, Nothing))
			Me.speechRecognizer.RecognizeAsync(RecognizeMode.Multiple)
			Dim t = New Thread(AddressOf Me.PollSoundSourceLocalization)
			t.Start()
		End Sub

		Private Sub EnableAecChecked(ByVal sender As Object, ByVal e As RoutedEventArgs)

		End Sub

		Private Sub PollSoundSourceLocalization()

		End Sub

		Private Class EnergyCalculatingPassThroughStream
			Inherits Stream
			Private Const SamplesPerPixel As Integer = 10

			Private ReadOnly energy(WaveImageWidth - 1) As Double
			Private ReadOnly syncRoot As New Object()
			Private ReadOnly baseStream As Stream

			Private index As Integer
			Private sampleCount As Integer
            Private avgSample As Double

			Public Sub New(ByVal stream As Stream)
				Me.baseStream = stream
			End Sub

			Public Overrides ReadOnly Property Length() As Long
				Get
					Return Me.baseStream.Length
				End Get
			End Property

			Public Overrides Property Position() As Long
				Get
					Return Me.baseStream.Position
				End Get
				Set(ByVal value As Long)
					Me.baseStream.Position = value
				End Set
			End Property

			Public Overrides ReadOnly Property CanRead() As Boolean
				Get
					Return Me.baseStream.CanRead
				End Get
			End Property

			Public Overrides ReadOnly Property CanSeek() As Boolean
				Get
					Return Me.baseStream.CanSeek
				End Get
			End Property

			Public Overrides ReadOnly Property CanWrite() As Boolean
				Get
					Return Me.baseStream.CanWrite
				End Get
			End Property

			Public Overrides Sub Flush()
				Me.baseStream.Flush()
			End Sub

			Public Sub GetEnergy(ByVal energyBuffer() As Double)
				SyncLock Me.syncRoot
					Dim energyIndex As Integer = Me.index
					For i As Integer = 0 To Me.energy.Length - 1
						energyBuffer(i) = Me.energy(energyIndex)
						energyIndex += 1
						If energyIndex >= Me.energy.Length Then
							energyIndex = 0
						End If
					Next i
				End SyncLock
			End Sub

			Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
				Dim retVal As Integer = Me.baseStream.Read(buffer, offset, count)
				Const A As Double = 0.3
				SyncLock Me.syncRoot
					For i As Integer = 0 To retVal - 1 Step 2
						Dim sample As Short = BitConverter.ToInt16(buffer, i + offset)

                        Me.avgSample += CInt(sample) * CInt(sample)
						Me.sampleCount += 1

						If Me.sampleCount = SamplesPerPixel Then
							Me.avgSample /= SamplesPerPixel

							Me.energy(Me.index) =.2 + ((Me.avgSample * 11) / (Integer.MaxValue / 2))
							Me.energy(Me.index) = If(Me.energy(Me.index) > 10, 10, Me.energy(Me.index))

							If Me.index > 0 Then
								Me.energy(Me.index) = (Me.energy(Me.index) * A) + ((1 - A) * Me.energy(Me.index - 1))
							End If

							Me.index += 1
							If Me.index >= Me.energy.Length Then
								Me.index = 0
							End If

							Me.avgSample = 0
							Me.sampleCount = 0
						End If
					Next i
				End SyncLock

				Return retVal
			End Function

			Public Overrides Function Seek(ByVal offset As Long, ByVal origin As SeekOrigin) As Long
				Return Me.baseStream.Seek(offset, origin)
			End Function

			Public Overrides Sub SetLength(ByVal value As Long)
				Me.baseStream.SetLength(value)
			End Sub

			Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
				Me.baseStream.Write(buffer, offset, count)
			End Sub
		End Class
	End Class
End Namespace
