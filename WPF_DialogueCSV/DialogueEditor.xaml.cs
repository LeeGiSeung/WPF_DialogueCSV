using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WPF_DialogueCSV
{
    public partial class DialogueEditor : Window
    {
        private string csvPath = @"C:\Users\82103\source\repos\WPF\DialogueCSV\WPF_DialogueCSV\Content\CSV";
        private string soundPath = @"C:\Users\82103\source\repos\WPF\DialogueCSV\WPF_DialogueCSV\Content\Sounds";

        public DialogueEditor()
        {
            InitializeComponent();
            LoadSoundList();
        }

        // 1. 사운드 목록 로드 (Speaker 콤보박스용)
        private void LoadSoundList()
        {
            try
            {
                if (Directory.Exists(soundPath))
                {
                    var files = Directory.GetFiles(soundPath)
                        .Select(Path.GetFileNameWithoutExtension)
                        .ToList();
                    files.Insert(0, "None");
                    ComboSpeaker.ItemsSource = files;
                    ComboSpeaker.SelectedIndex = 0;
                }
            }
            catch (Exception ex) { MessageBox.Show("사운드 로드 실패: " + ex.Message); }
        }

        // 2. CSV 로드 (Alter 영역)
        private void LoadCsvData()
        {
            if (csvList.SelectedItem is ComboBoxItem item)
            {
                string fileName = item.Content.ToString();

                if (fileName == "Choice")
                {
                    ChoiceSection.Visibility = Visibility.Visible;
                }
                else
                {
                    ChoiceSection.Visibility = Visibility.Collapsed;
                }


                    string fullPath = Path.Combine(csvPath, $"{fileName}.csv");
                if (!File.Exists(fullPath)) return;

                DataTable dt = new DataTable();
                string[] lines = File.ReadAllLines(fullPath, Encoding.UTF8);

                if (lines.Length > 0)
                {
                    string[] headers = lines[0].Split(',');
                    // DataTable은 중복 컬럼명을 허용하지 않으므로 처리 (ID, ID 대응)
                    for (int i = 0; i < headers.Length; i++)
                    {
                        string hName = headers[i].Trim();
                        if (dt.Columns.Contains(hName)) dt.Columns.Add($"{hName}_{i}");
                        else dt.Columns.Add(hName);
                    }

                    for (int i = 1; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        string[] data = lines[i].Split(',');
                        // 열 개수 맞춤
                        string[] safeData = new string[dt.Columns.Count];
                        for (int j = 0; j < dt.Columns.Count; j++)
                            safeData[j] = j < data.Length ? data[j].Trim() : "None";
                        dt.Rows.Add(safeData);
                    }
                }
                dgDisplay.ItemsSource = dt.DefaultView;
            }
        }

        // 3. 표에서 행 선택 시 에디터로 복사
        private void dgDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDisplay.SelectedItem is DataRowView row)
            {
                string currentFile = (csvList.SelectedItem as ComboBoxItem).Content.ToString();

                // 공통: ID (숫자만 추출)
                TxtID.Text = row[0].ToString().Split('_').Last();
                TxtFirst.Text = row["FirstText"].ToString();
                TxtSecond.Text = row["SecondText"].ToString();
                TxtDirectingKey.Text = row["DirectingKey"].ToString();
                ComboUIType.Text = row["UIType"].ToString();

                if (currentFile == "Choice")
                {
                    // Choice 전용 필드 복사
                    TxtChoice1.Text = row["ChoiceText1"].ToString();
                    TxtChoice2.Text = row["ChoiceText2"].ToString();
                    TxtChoice3.Text = row["ChoiceText3"].ToString();
                    TxtChoice4.Text = row["ChoiceText4"].ToString();

                    // Answer ID 숫자만 추출
                    TxtAnswer1.Text = row["ChoiceTextAnswer1"].ToString().Split('_').Last();
                    TxtAnswer2.Text = row["ChoiceTextAnswer2"].ToString().Split('_').Last();
                    TxtAnswer3.Text = row["ChoiceTextAnswer3"].ToString().Split('_').Last();
                    TxtAnswer4.Text = row["ChoiceTextAnswer4"].ToString().Split('_').Last();

                    ComboSpeaker.Text = row["Speaker"].ToString();
                }
                else
                {
                    ComboSpeaker.Text = row["Speaker"].ToString();
                    string nId = row["NextID"].ToString();
                    TxtNextID.Text = nId.Contains("_") ? nId.Split('_').Last() : nId;
                }
            }
        }

        // 4. 저장 버튼
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string targetFile = (csvList.SelectedItem as ComboBoxItem)?.Content.ToString();
            string nextType = (ComboUIType.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (targetFile == null || nextType == null) return;

            string myID = $"ID_{targetFile.ToUpper()}_{TxtID.Text.Trim()}";
            string speaker = string.IsNullOrEmpty(ComboSpeaker.Text) ? "None" : ComboSpeaker.Text;
            string dKey = string.IsNullOrEmpty(TxtDirectingKey.Text) ? "None" : TxtDirectingKey.Text;

            List<string> rowData = new List<string>();

            if (targetFile == "Choice")
            {
                // Choice 구조 (15개 컬럼)
                rowData.Add(myID); // RowName
                rowData.Add(myID); // ID
                rowData.Add(TxtFirst.Text);
                rowData.Add(TxtSecond.Text);
                rowData.Add(string.IsNullOrEmpty(TxtChoice1.Text) ? "None" : TxtChoice1.Text);
                rowData.Add(string.IsNullOrEmpty(TxtChoice2.Text) ? "None" : TxtChoice2.Text);
                rowData.Add(string.IsNullOrEmpty(TxtChoice3.Text) ? "None" : TxtChoice3.Text);
                rowData.Add(string.IsNullOrEmpty(TxtChoice4.Text) ? "None" : TxtChoice4.Text);

                // Answer ID 자동 포맷팅 (숫자 입력 시 ID_CHOICE_숫자)
                rowData.Add(FormatChoiceID(TxtAnswer1.Text, nextType));
                rowData.Add(FormatChoiceID(TxtAnswer2.Text, nextType));
                rowData.Add(FormatChoiceID(TxtAnswer3.Text, nextType));
                rowData.Add(FormatChoiceID(TxtAnswer4.Text, nextType));

                rowData.Add(nextType);
                rowData.Add(speaker);
                rowData.Add(dKey);
            }
            else
            {
                // Normal/Auto/Cinematic 구조 (8개 컬럼)
                string nextID = (nextType == "End") ? "None" : $"ID_{nextType.ToUpper()}_{TxtNextID.Text.Trim()}";
                rowData.AddRange(new[] { myID, myID, speaker, TxtFirst.Text, TxtSecond.Text, nextType, nextID, dKey });
            }

            // 쉼표 제거 처리 후 합치기
            string finalLine = string.Join(",", rowData.Select(s => s.Replace(",", " ").Trim()));
            string fullPath = Path.Combine(csvPath, $"{targetFile}.csv");

            try
            {
                var lines = File.ReadAllLines(fullPath, Encoding.UTF8).ToList();
                int idx = lines.FindIndex(l => l.StartsWith(myID + ","));
                if (idx >= 0) lines[idx] = finalLine;
                else lines.Add(finalLine);

                File.WriteAllLines(fullPath, lines, Encoding.UTF8);
                LoadCsvData();
                MessageBox.Show("저장 완료!");
            }
            catch (Exception ex) { MessageBox.Show("저장 실패: " + ex.Message); }
        }

        private string FormatChoiceID(string input, string prefix)
        {
            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "none") return "None";

            // 숫자인 경우에만 ID_타입_숫자 형식으로 만들고, 아니면 그대로 반환
            return int.TryParse(input, out _) ? $"ID_{prefix.ToUpper()}_{input.Trim()}" : input.Trim();
        }

        //private string FormatChoiceID(string input)
        //{
        //    if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "none") return "None";
        //    return int.TryParse(input, out _) ? $"ID_CHOICE_{input.Trim()}" : input.Trim();
        //}

        private void csvList_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadCsvData();
        private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadCsvData();

        private void ComboUIType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChoiceSection == null) return;
            string type = (ComboUIType.SelectedItem as ComboBoxItem)?.Content.ToString();
            // Next UI Type이 Choice거나, 현재 편집 중인 파일 자체가 Choice인 경우 보이기
            string currentFile = (csvList.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (type == "Choice" || currentFile == "Choice")
                ChoiceSection.Visibility = Visibility.Visible;
            else
                ChoiceSection.Visibility = Visibility.Collapsed;
        }

        private void bntClear_Click(object sender, RoutedEventArgs e)
        {
            TxtID.Clear(); TxtFirst.Clear(); TxtSecond.Clear(); TxtNextID.Clear(); TxtDirectingKey.Clear();
            TxtChoice1.Clear(); TxtChoice2.Clear(); TxtChoice3.Clear(); TxtChoice4.Clear();
            TxtAnswer1.Clear(); TxtAnswer2.Clear(); TxtAnswer3.Clear(); TxtAnswer4.Clear();
        }

        private void btnPlaySound_Click(object sender, RoutedEventArgs e)
        {
            // 1. 현재 선택된 텍스트 가져오기
            string selectedSpeaker = ComboSpeaker.Text.Trim();

            if (string.IsNullOrEmpty(selectedSpeaker) || selectedSpeaker.ToLower() == "none")
            {
                MessageBox.Show("재생할 사운드가 선택되지 않았습니다.");
                return;
            }

            // 2. 경로 조합 (soundPath는 이미 정의된 변수 사용)
            // 확장자가 포함되어 있지 않다면 .wav를 붙여줍니다.
            string fullSoundPath = Path.Combine(soundPath, selectedSpeaker);

            if (!fullSoundPath.EndsWith(".wav"))
            {
                fullSoundPath += ".wav";
            }

            // 3. 파일 존재 여부 확인 후 재생
            if (File.Exists(fullSoundPath))
            {
                try
                {
                    SoundPlayer player = new SoundPlayer(fullSoundPath);
                    player.Play(); // 비동기 재생 (UI 안 멈춤)
                }
                catch (Exception ex)
                {
                    MessageBox.Show("재생 오류: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show($"파일을 찾을 수 없습니다:\n{fullSoundPath}");
            }
        }

    }
}