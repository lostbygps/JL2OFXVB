Class MainWindow
    Private Function BrowseForFile(CurrentFile As String, filter As String, title As String)

        Dim OpenFileDialog As New System.Windows.Forms.OpenFileDialog With {
            .InitialDirectory = System.IO.Path.GetDirectoryName(CurrentFile.TrimEnd()),
            .Title = title,
            .Filter = filter,
            .FileName = System.IO.Path.GetFileName(CurrentFile)
        }
        Return If(OpenFileDialog.ShowDialog() = vbCancel, CurrentFile, OpenFileDialog.FileName)
    End Function

    Private Sub Validate_and_load_file(InputFile As String)

        InvalidCSV.Visibility = Visibility.Hidden
        GridData.Clear()
        CSVFileDetail.Items.Refresh()
        ProcessButton.IsEnabled = True
        StatementDate.Visibility = Visibility.Visible
        CSVGrid.SetRow(CSVFileDetail, 1)

        Try
            Using StatementDataIn As New Microsoft.VisualBasic.FileIO.TextFieldParser(InputFile)
                StatementDataIn.TextFieldType = FileIO.FieldType.Delimited
                StatementDataIn.SetDelimiters(",")
                Dim currentRow As String(), Rownum As Integer = 0
                While Not StatementDataIn.EndOfData
                    currentRow = StatementDataIn.ReadFields()
                    Rownum = Rownum + 1
                    Select Case True
                        Case Rownum = 2
                            StatementDate.Content = "Statement Date: " + currentRow(1)
                        Case Rownum > 3
                            Dim Gridentry As New StatementData(currentRow(0), currentRow(1), currentRow(2), currentRow(3))
                            GridData.Add(Gridentry)
                    End Select
                End While
                If Rownum < 3 Then
                    Throw New System.Exception("Input file does not contain transaction records")
                End If
            End Using

        Catch
            InvalidCSV.Visibility = Visibility.Visible
            ProcessButton.IsEnabled = False
            StatementDate.Visibility = Visibility.Hidden
            CSVGrid.SetRow(CSVFileDetail, 0)
        End Try
    End Sub

    Private Sub GenerateOFXFile(Infile As String, Outfile As String)

        Dim Regex As New Text.RegularExpressions.Regex("")
        JL2OFX.Cursor = Input.Cursors.Wait
        JL2OFX.Dispatcher.Invoke(Threading.DispatcherPriority.Background, New Action(Sub()

                                                                                     End Sub))
        Progress.Visibility = Visibility.Visible
        Progress.Value = 0
        Progress.Foreground = (New System.Windows.Media.BrushConverter).ConvertFromString("Green")

        ProcessCancel.IsEnabled = False
        JL2OFX.Dispatcher.Invoke(Threading.DispatcherPriority.Background, New Action(Sub()

                                                                                     End Sub))

        Try
            ' Create The OFX Document
            Dim XmlWriter As New System.Xml.XmlTextWriter(Outfile, Nothing)

            ' Set The Formatting
            XmlWriter.Formatting = Xml.Formatting.Indented
            XmlWriter.Indentation = "4"

            XmlWriter.WriteProcessingInstruction("xml", "version=""1.0"" encoding=""utf-8"" standalone=""yes""")
            XmlWriter.WriteProcessingInstruction("OFX", "OFXHEADER=""200"" VERSION=""203"" SECURITY=""NONE"" OLDFILEUID=""NONE"" NEWFILEUID=""NONE""")

            Dim RUNDATE = System.DateTimeOffset.Now.ToString("yyyyMMddHHmmss.fff[z]")
            Dim DateRange = Aggregate StatementEntry As StatementData In GridData
                        Into MaxDate = Max(StatementEntry.DateProcessed), MinDate = Min(StatementEntry.DateProcessed)

            XmlWriter.WriteStartElement("OFX")
            XmlWriter.WriteStartElement("SIGNONMSGSRSV1")
            XmlWriter.WriteStartElement("SONRS")
            XmlWriter.WriteStartElement("STATUS")
            XmlWriter.WriteElementString("CODE", "0")
            XmlWriter.WriteElementString("INFO", "INFO")
            XmlWriter.WriteEndElement()
            XmlWriter.WriteElementString("DTSERVER", RUNDATE)
            XmlWriter.WriteElementString("LANGUAGE", "ENG")
            XmlWriter.WriteEndElement()
            XmlWriter.WriteEndElement()
            XmlWriter.WriteStartElement("CREDITCARDMSGSRSV1")
            XmlWriter.WriteStartElement("CCSTMTTRNRS")
            XmlWriter.WriteElementString("TRNUID", "A")
            XmlWriter.WriteStartElement("STATUS")
            XmlWriter.WriteElementString("CODE", "0")
            XmlWriter.WriteElementString("INFO", "INFO")
            XmlWriter.WriteEndElement()
            XmlWriter.WriteStartElement("CCSTMTRS")
            XmlWriter.WriteElementString("CURDEF", "GBP")
            XmlWriter.WriteStartElement("CCACCTFROM")
            XmlWriter.WriteElementString("ACCTID", "************0271")
            XmlWriter.WriteEndElement()

            XmlWriter.WriteStartElement("BANKTRANLIST")
            XmlWriter.WriteElementString("DTSTART", String.Format("{0:yyyyMMddHHmmss.fff[z]}", DateRange.MinDate))
            XmlWriter.WriteElementString("DTEND", String.Format("{0:yyyyMMddHHmmss.fff[z]}", DateRange.MaxDate))

            Dim TransTotal As Double = 0
            Dim ProgressCount As Integer = 0
            Dim POSTAMNT As Double, TRNTYPE As String, TRNMULTIPLIER As Integer

            For Each Transaction As StatementData In GridData

                ProgressCount = ProgressCount + 1

                Progress.Value = (ProgressCount / GridData.Count) * 100
                JL2OFX.Dispatcher.Invoke(Threading.DispatcherPriority.Background, New Action(Sub()

                                                                                             End Sub))
                XmlWriter.WriteStartElement("STMTTRN")
                Dim POSTDATE = New System.DateTimeOffset(Transaction.DateProcessed)
                POSTAMNT = Transaction.Amount
                TRNTYPE = "POS"
                TRNMULTIPLIER = 1
                If Transaction.Credit Then
                    TRNTYPE = "CREDIT"
                End If
                POSTAMNT = Transaction.Amount * -1

                If (Transaction.Description <> "PAYMENT RECEIVED - THANK YOU") Then
                    TransTotal = TransTotal + (Transaction.Amount * -1)
                End If
                XmlWriter.WriteElementString("DTPOSTED", POSTDATE.ToString("yyyyMMddHHmmss.fff[z]"))
                XmlWriter.WriteElementString("TRNAMT", POSTAMNT.ToString())
#Disable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
                XmlWriter.WriteElementString("FITID", Regex.Replace(String.Format("00{4}{0:yyyyMMddHHmmssfff}{1}{2}{3}", POSTDATE, Regex.Replace(String.Format("{0:zz}", POSTDATE), "[^\d]", ""),
                            Regex.Replace(POSTAMNT.ToString(), "\.", ""), Transaction.Description, TRNTYPE), " ", ""))
#Enable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
                XmlWriter.WriteElementString("NAME", Transaction.Description)
                XmlWriter.WriteEndElement()
            Next

            XmlWriter.WriteEndElement()
            XmlWriter.WriteStartElement("LEDGERBAL")
            XmlWriter.WriteElementString("BALAMT", TransTotal)
            XmlWriter.WriteElementString("DTASOF", String.Format("{0:yyyyMMddHHmmss.fff[z]}", DateRange.MaxDate))
            XmlWriter.WriteEndElement()
            XmlWriter.WriteStartElement("AVAILBAL")
            XmlWriter.WriteElementString("BALAMT", (Creditlimit + TransTotal))
            XmlWriter.WriteElementString("DTASOF", (String.Format("{0:yyyyMMddHHmmss.fff[z]}", DateRange.MinDate)))
            XmlWriter.WriteEndElement()
            XmlWriter.WriteEndElement()
            XmlWriter.WriteEndElement()
            XmlWriter.WriteEndElement()
            XmlWriter.WriteEndElement()

            ' Finish The Document
            XmlWriter.Flush()
            XmlWriter.Close()

        Catch ex As Exception

            Progress.Value = 100
            Progress.Foreground = (New System.Windows.Media.BrushConverter).ConvertFromString("Red")
            ToPath.Text = ex.Message
            Media.SystemSounds.Exclamation.Play()
        End Try

        JL2OFX.Cursor = Windows.Input.Cursors.Arrow
        ProcessCancel.IsEnabled = True
        JL2OFX.Dispatcher.Invoke(Threading.DispatcherPriority.Background, New Action(Sub()

                                                                                     End Sub))
    End Sub


    Dim FromPathText, ToPathText As String, GridData As New ArrayList
    Const Creditlimit As Double = 6500

    Private Sub ToPath_Loaded(sender As Object, e As RoutedEventArgs) Handles ToPath.Loaded
        ToPath.Text = ToPathText
    End Sub

    Private Sub BrowseSource_Click(sender As Object, e As RoutedEventArgs) Handles BrowseSource.Click
        Progress.Visibility = Visibility.Hidden
        FromPath.Text = BrowseForFile(FromPath.Text, "CSV Files (*.csv)|*.csv", "Select input file")
        ToPath.Text = IO.Path.ChangeExtension(FromPath.Text, ".ofx")
        Validate_and_load_file(FromPath.Text)
        CSVFileDetail.ItemsSource = GridData
    End Sub

    Private Sub ProcessCancel_Click(sender As Object, e As RoutedEventArgs) Handles ProcessCancel.Click
        Me.Close()
    End Sub

    Private Sub ProcessButton_Click(sender As Object, e As RoutedEventArgs) Handles ProcessButton.Click
        GenerateOFXFile(FromPath.Text, ToPath.Text)
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub FromPath_Loaded(sender As Object, e As RoutedEventArgs) Handles FromPath.Loaded
        Dim frompathdir As New IO.DirectoryInfo(Environment.ExpandEnvironmentVariables("%USERPROFILE%\downloads"))
        Dim frompathlist As System.IO.FileInfo() = frompathdir.GetFiles("*.csv")
        If frompathlist.Length > 0 Then
            FromPathText = (From filedets In frompathlist
                            Order By filedets.LastAccessTime Descending, filedets.FullName
                            Select filedets.FullName).First()
            ToPathText = IO.Path.ChangeExtension(FromPathText, ".ofx")
        Else
            FromPathText = ""
            ToPathText = ""
        End If
        FromPath.Text = FromPathText
        Validate_and_load_file(FromPathText)
        CSVFileDetail.ItemsSource = GridData
    End Sub


End Class
