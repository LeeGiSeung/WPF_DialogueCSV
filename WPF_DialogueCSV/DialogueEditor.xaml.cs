using System;
using System.IO;        // 파일 및 경로 처리를 위해 필수
using System.Linq;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WPF_DialogueCSV
{
    public partial class DialogueEditor : Window
    {
        public enum EDialogueUIType { Normal, Choice, Auto, Cinematic, End }
        public enum EDialogueEventType { Normal, CameraSlowAroundMove }

        // 오디오 파일이 들어있는 실제 폴더 경로 (본인의 경로에 맞게 수정하세요)
        private string soundFolderPath = @"C:\Users\82103\source\repos\WPF\DialogueCSV\WPF_DialogueCSV\Content\Sounds";
        private readonly string csvFolderPath = @"C:\Users\82103\source\repos\WPF\DialogueCSV\WPF_DialogueCSV\Content\CSV";

        private Dictionary<string, List<string[]>> _csvContents = new Dictionary<string, List<string[]>>();
        private void LoadAllCsvFiles()
        {
            string[] fileTypes = { "Auto", "Normal", "Choice", "Cinematic" };
            foreach (var type in fileTypes)
            {
                string filePath = Path.Combine(csvFolderPath, $"{type}.csv");
                _csvContents[type] = new List<string[]>();

                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        // 쉼표로 분리 (실제 CSV는 따옴표 처리가 필요할 수 있으나 기본 분리 적용)
                        _csvContents[type].Add(line.Split(','));
                    }
                }
            }
        }

        public DialogueEditor()
        {
            InitializeComponent(); // 한 번만 호출해야 합니다.

            LoadAllCsvFiles();

            // 1. Enum 데이터 연결
            ComboUIType.ItemsSource = Enum.GetValues(typeof(EDialogueUIType));
            ComboEventType.ItemsSource = Enum.GetValues(typeof(EDialogueEventType));

            // 2. 오디오 파일 목록 불러오기
            RefreshSpeakerList();

            // 기본값 설정
            ComboUIType.SelectedIndex = 0;
            ComboEventType.SelectedIndex = 0;
        }

        // 특정 폴더에서 파일 목록을 가져와 ComboBox에 채우는 함수
        private void RefreshSpeakerList()
        {
            if (Directory.Exists(soundFolderPath))
            {
                // .wav 파일들만 찾아서 리스트업
                var files = Directory.GetFiles(soundFolderPath, "*.wav")
                                     .Select(f => Path.GetFileNameWithoutExtension(f))
                                     .ToList();
                ComboSpeaker.ItemsSource = files;
            }
            else
            {
                MessageBox.Show("오디오 폴더 경로가 잘못돼있습니다.");
            }
        }

        private void ComboUIType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboUIType.SelectedItem == null) return;
            var selected = (EDialogueUIType)ComboUIType.SelectedItem;

            if (ChoiceSection != null)
            {
                ChoiceSection.Visibility = (selected == EDialogueUIType.Choice)
                           ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // 스피커를 선택했을 때 또는 재생 버튼을 눌렀을 때 소리 재생
        private void btnPlaySound_Click(object sender, RoutedEventArgs e)
        {
            if (ComboSpeaker.SelectedItem == null) return;

            string selectedSpeaker = ComboSpeaker.SelectedItem.ToString();

            if (string.IsNullOrEmpty(selectedSpeaker))
            {
                // 선택된 게 없으면 재생하지 않고 함수 종료
                return;
            }
            string filePath = Path.Combine(soundFolderPath, selectedSpeaker + ".wav");

            if (File.Exists(filePath))
            {
                try
                {
                    SoundPlayer player = new SoundPlayer(filePath);
                    player.Play();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("재생 중 오류 발생: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("해당하는 음성 파일이 없습니다.");
            }
        }

        private void bntClear_Click(object sender, RoutedEventArgs e)
        {
            ClearChoiceInput();
        }

        private void ClearChoiceInput()
        {
            TxtID.Clear();
            TxtFirst.Clear();
            TxtSecond.Clear();
            TxtDirectingKey.Clear();
            TxtNextID.Clear();

            ComboSpeaker.SelectedIndex = -1;

            TxtChoice1.Clear(); TxtAnswer1.Clear();
            TxtChoice2.Clear(); TxtAnswer2.Clear();
            TxtChoice3.Clear(); TxtAnswer3.Clear();
            TxtChoice4.Clear(); TxtAnswer4.Clear();
        }

        private void TxtFileName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 1. 저장할 파일 및 현재 ID 규칙 (csvList 기준)
            string selectedFile = (csvList.SelectedItem is ComboBoxItem cItem) ? cItem.Content.ToString() : csvList.Text;

            // 2. 데이터 내부에 기록될 UI Type (ComboUIType 기준)
            string dataUIType = (ComboUIType.SelectedItem is ComboBoxItem uItem) ? uItem.Content.ToString() : ComboUIType.Text;

            if (string.IsNullOrEmpty(selectedFile) || string.IsNullOrEmpty(dataUIType))
            {
                MessageBox.Show("파일 리스트와 Next UI Type을 모두 선택해주세요.");
                return;
            }

            // 3. ID 포매팅 (현재 저장되는 파일 타입 기준)
            string rawId = TxtID.Text.Trim();
            string formattedID = $"ID_{selectedFile.ToUpper()}_{rawId}";

            // 4. Next ID 포매팅 (ComboUIType 기준)
            // TxtNextID에 숫자만 쓰면 "ID_선택한NextUIType_숫자" 로 저장
            string rawNextId = TxtNextID.Text.Trim();
            string formattedNextID = "None";
            if (!string.IsNullOrEmpty(rawNextId) && rawNextId.ToLower() != "none")
            {
                formattedNextID = int.TryParse(rawNextId, out _)
                    ? $"ID_{dataUIType.ToUpper()}_{rawNextId}"
                    : rawNextId;
            }

            // 5. 공통 데이터 정리
            string speakerID = string.IsNullOrEmpty(ComboSpeaker.Text) ? "None" : ComboSpeaker.Text;
            string directingKey = string.IsNullOrEmpty(TxtDirectingKey.Text) ? "None" : TxtDirectingKey.Text;
            string firstText = TxtFirst.Text.Replace("\r\n", " ").Replace(",", " ");
            string secondText = TxtSecond.Text.Replace("\r\n", " ").Replace(",", " ");

            // 6. 열(Column) 구성 분기 (저장될 파일의 형식을 따름)
            List<string> rowData = new List<string>();

            if (selectedFile == "Choice")
            {
                // Choice 파일 형식
                rowData.Add(formattedID); rowData.Add(formattedID);
                rowData.Add(firstText); rowData.Add(secondText);
                rowData.Add(string.IsNullOrEmpty(TxtChoice1.Text) ? "None" : TxtChoice1.Text.Replace(",", " "));
                rowData.Add(string.IsNullOrEmpty(TxtChoice2.Text) ? "None" : TxtChoice2.Text.Replace(",", " "));
                rowData.Add(string.IsNullOrEmpty(TxtChoice3.Text) ? "None" : TxtChoice3.Text.Replace(",", " "));
                rowData.Add(string.IsNullOrEmpty(TxtChoice4.Text) ? "None" : TxtChoice4.Text.Replace(",", " "));
                rowData.Add(string.IsNullOrEmpty(TxtAnswer1.Text) ? "None" : TxtAnswer1.Text.Replace(",", " "));
                rowData.Add(string.IsNullOrEmpty(TxtAnswer2.Text) ? "None" : TxtAnswer2.Text.Replace(",", " "));
                rowData.Add(string.IsNullOrEmpty(TxtAnswer3.Text) ? "None" : TxtAnswer3.Text.Replace(",", " "));
                rowData.Add(string.IsNullOrEmpty(TxtAnswer4.Text) ? "None" : TxtAnswer4.Text.Replace(",", " "));
                rowData.Add(dataUIType); // 데이터 내 UI Type
                rowData.Add(speakerID);
                rowData.Add(directingKey);
            }
            else
            {
                // Normal, Auto, Cinematic 파일 형식
                rowData.Add(formattedID);
                rowData.Add(formattedID);
                rowData.Add(speakerID);
                rowData.Add(firstText);
                rowData.Add(secondText);
                rowData.Add(dataUIType); // 데이터 내 UI Type
                rowData.Add(formattedNextID); // 포매팅된 Next ID
                rowData.Add(directingKey);
            }

            string finalCsvLine = string.Join(",", rowData);

            try
            {
                string csvFolderPath = @"C:\Users\82103\source\repos\WPF\DialogueCSV\WPF_DialogueCSV\Content\CSV";
                string fullPath = Path.Combine(csvFolderPath, $"{selectedFile}.csv");

                if (!File.Exists(fullPath))
                {
                    MessageBox.Show($"{selectedFile}.csv 파일이 없습니다.");
                    return;
                }

                List<string> allLines = File.ReadAllLines(fullPath, Encoding.UTF8).ToList();
                int existingIndex = allLines.FindIndex(line => line.StartsWith(formattedID + ","));

                if (existingIndex >= 0)
                {
                    allLines[existingIndex] = finalCsvLine;
                    File.WriteAllLines(fullPath, allLines, Encoding.UTF8);
                    MessageBox.Show($"{selectedFile} 파일 수정 완료: {formattedID}");
                }
                else
                {
                    allLines.Add(finalCsvLine);
                    File.WriteAllLines(fullPath, allLines, Encoding.UTF8);
                    MessageBox.Show($"{selectedFile} 파일 추가 완료: {formattedID}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}");
            }
        }

        private void AutoIncrementID()
        {
            if (int.TryParse(TxtID.Text, out int currentID))
            {
                TxtID.Text = (currentID + 1).ToString();
            }
        }

        private string EscapeCsv(string data)
        {
            if (string.IsNullOrEmpty(data)) return "";
            // 데이터에 쉼표나 큰따옴표가 있으면 큰따옴표로 감싸줌
            if (data.Contains(",") || data.Contains("\""))
            {
                return $"\"{data.Replace("\"", "\"\"")}\"";
            }
            return data;
        }

        private void TxtID_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }


}