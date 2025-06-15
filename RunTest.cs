using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using static System.Net.Mime.MediaTypeNames;

namespace WinFormsApp1
{
    public partial class RunTest : Form
    {
        private List<Question> questions = new List<Question>(); // Список вопросов
        private int currentQuestionIndex = 0;
        private int score = 0; // Переменная для хранения набранных баллов
        private int totalQuestions;
        private Panel questionPanel;

        private int _testId;
        private int userId;
        public RunTest(int testId, string testName, string subject, int questionCount, int userId)
        {
            this.userId = userId;
            _testId = testId;

            InitializeComponent();
            InitializeQuestionPanel(); // Инициализация панели вопросов
            LoadTestInfo(testId, testName, subject, questionCount);
            InitializeInstructionTest(testId);
        }

        private void InitializeTestPanel()
        {
            Test = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(1028, 559), // Установите нужный размер
                AutoScroll = false // Включите прокрутку, если нужно
            };
            this.Controls.Add(Test); // Добавляем панель на форму
        }

        private void LoadTestInfo(int testId, string testName, string subject, int questionCount)
        {
            // Создаем Label для отображения информации о тесте
            Label testNameLabel = new Label() { Text = "Название теста", AutoSize = true, Location = new Point(43, 26) };
            TextBox testNameTextBox = new TextBox() { Text = testName, Width = 355, Height = 20, Location = new Point(43, 44), ReadOnly = true, };
            Label subjectLabel = new Label() { Text = "Тема теста", AutoSize = true, Location = new Point(424, 26) };
            TextBox subjectTextBox = new TextBox() { Text = subject, Width = 250, Height = 20, Location = new Point(424, 44), ReadOnly = true };
            Label questionCountLabel = new Label() { Text = "Количество вопросов", AutoSize = true, Location = new Point(700, 26) };
            TextBox questionCountTextBox = new TextBox() { Text = questionCount.ToString(), Width = 150, Height = 20, Location = new Point(700, 44), ReadOnly = true };

            // Добавляем Labels на панель Info (предполагается, что у вас есть панель с именем InfoPanel)
            Info.Controls.Add(testNameLabel);
            Info.Controls.Add(testNameTextBox);
            Info.Controls.Add(subjectLabel);
            Info.Controls.Add(subjectTextBox);
            Info.Controls.Add(questionCountLabel);
            Info.Controls.Add(questionCountTextBox);

            // Здесь можно загрузить вопросы из базы данных
            LoadQuestions(testId);
        }

        private void StartTestButton_Click(object sender, EventArgs e)
        {
            this.Controls.Clear();

            this.Controls.Add(Test); // Если Test — это панель с тестом

            // Здесь можно вызвать метод для отображения первого вопроса
            DisplayCurrentQuestion();

            // Загружаем вопросы, передавая идентификатор теста
            LoadQuestions(1023); // Убедитесь, что testId определен и содержит правильное значение
        }

        // Метод для инициализации инструкций и кнопки
        private void InitializeInstructionTest(int testId)
        {
            Label instructionLabel = new Label();
            instructionLabel.Text = "Инструкции по прохождению теста:\n" +
                                    "Добро пожаловать в тест! Пожалуйста, внимательно прочитайте следующие инструкции перед началом.\n" +
                                    "•  Время прохождения теста: Время не ограничено. Вы можете проходить тест в удобном для вас темпе. Однако старайтесь не затягивать с ответами,\n" +
                                    "чтобы сохранить фокус и эффективность.\n" +
                                    "•  Типы вопросов: Тест состоит из различных типов вопросов, включая:\n" +
                                    "1. Один ответ: Выберите один правильный ответ из предложенных вариантов.\n" +
                                    "2. Несколько ответов: Выберите все правильные ответы из предложенных вариантов.\n" +
                                    "3. Последовательность: Упорядочите элементы в правильной последовательности.\n" +
                                    "•  После того как вы ответите на все вопросы, нажмите кнопку \"Завершить тест\", чтобы отправить свои ответы на проверку.\n" +
                                    "Желаем удачи в прохождении теста!";
            instructionLabel.AutoSize = true;
            instructionLabel.Location = new Point(10, 10);

            Button startTestButton = new Button();
            startTestButton.Text = "Начать тест";
            startTestButton.Location = new Point(10, 200);

            // Привязываем обработчик события к кнопке
            startTestButton.Click += new EventHandler(StartTestButton_Click);

            // Добавляем инструкции и кнопку на панель Test
            Test.Controls.Add(instructionLabel);
            Test.Controls.Add(startTestButton);

            // Добавляем панель Test на форму (если это еще не сделано)
            this.Controls.Add(Test);
        }

        public class Question
        {
            public string QuestionText { get; set; }
            public int QuestionID { get; set; }
            public List<Answer> Answers { get; set; } = new List<Answer>();
            public QuestionType QuestionType { get; set; } // Добавьте это поле для типа вопроса
        }

        public enum QuestionType
        {
            SingleChoice,
            MultipleChoice,
            Sequence
        }
        public class Answer
        {
            public int AnswerID { get; set; }
            public int QuestionID { get; set; }
            public string AnswerText { get; set; }
            public bool IsCorrect { get; set; }
        }

        private QuestionType? GetQuestionTypeFromString(string questionTypeString)
        {
            return questionTypeString switch
            {
                "Один ответ" => QuestionType.SingleChoice,
                "Несколько ответов" => QuestionType.MultipleChoice,
                "Последовательность ответов" => QuestionType.Sequence,
                _ => null // Возвращаем null, если тип не найден
            };
        }

        [Obsolete]
        private void LoadQuestions(int testId)
        {
            using (DB db = new DB())
            {
                try
                {
                    db.OpenConnection();

                    string queryQuestions = "SELECT * FROM Questions WHERE TestID = @TestID";
                    SqlCommand command = new SqlCommand(queryQuestions, db.GetConnection());
                    command.Parameters.AddWithValue("@TestID", testId);

                    SqlDataReader reader = command.ExecuteReader();

                    questions.Clear(); // Очищаем старые вопросы

                    while (reader.Read())
                    {
                        if (reader["QuestionText"] != DBNull.Value && reader["QuestionType"] != DBNull.Value)
                        {
                            Question question = new Question
                            {
                                QuestionText = reader["QuestionText"].ToString(),
                                QuestionID = (int)reader["QuestionID"]
                            };

                            string questionTypeString = reader["QuestionType"].ToString();
                            var questionType = GetQuestionTypeFromString(questionTypeString);

                            if (questionType.HasValue)
                            {
                                question.QuestionType = questionType.Value;
                            }

                            questions.Add(question);
                        }
                    }

                    reader.Close();

                    foreach (var question in questions)
                    {
                        string queryAnswers = "SELECT * FROM Answers WHERE QuestionID = @QuestionID";
                        SqlCommand answerCommand = new SqlCommand(queryAnswers, db.GetConnection());
                        answerCommand.Parameters.AddWithValue("@QuestionID", question.QuestionID);

                        SqlDataReader answerReader = answerCommand.ExecuteReader();

                        while (answerReader.Read())
                        {
                            Answer answer = new Answer
                            {
                                AnswerID = (int)answerReader["AnswerID"],
                                QuestionID = (int)answerReader["QuestionID"],
                                AnswerText = answerReader["AnswerText"].ToString(),
                                IsCorrect = (bool)answerReader["IsCorrect"]
                            };
                            question.Answers.Add(answer);
                        }
                        answerReader.Close();
                    }

                    db.CloseConnection();

                    if (questions.Count > 0)
                    {
                        currentQuestionIndex = 0;
                        DisplayCurrentQuestion(); // Отображаем первый вопрос после загрузки
                    }
                    else
                    {
                        MessageBox.Show("Нет доступных вопросов для данного теста.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке вопросов: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DisplayCurrentQuestion()
        {
            if (currentQuestionIndex >= 0 && currentQuestionIndex < questions.Count)
            {
                Question currentQuestion = questions[currentQuestionIndex];
                // Обновите текст вопроса, если это необходимо
                Test.Text = currentQuestion.QuestionText;

                questionPanel.Controls.Clear(); // Очищаем только панель с вопросами
                int currentY = 10; // Начальная позиция по Y

                TextBox textBox = new TextBox
                {
                    Text = currentQuestion.QuestionText,
                    AutoSize = true,
                    Location = new Point(10, currentY),
                    Height = 20,
                    Width = 600,
                    ReadOnly = true
                };
                questionPanel.Controls.Add(textBox);
                currentY += textBox.Height + 10;
                switch (currentQuestion.QuestionType)
                {
                    case QuestionType.SingleChoice:
                        foreach (var answer in currentQuestion.Answers)
                        {
                            TextBox textbox = new TextBox
                            {
                                Text = answer.AnswerText,
                                AutoSize = true,
                                Location = new Point(10, currentY),
                                Height = 20,
                                Width = 600,
                                ReadOnly = true
                            };

                            RadioButton radioButton = new RadioButton
                            {
                                Tag = answer,
                                Location = new Point(textbox.Right + 5, textbox.Top)
                            };

                            Test.Controls.Add(textbox);
                            Test.Controls.Add(radioButton);

                            currentY += textbox.Height + 10; // Обновляем позицию Y для следующего элемента
                        }
                        break;
                    case QuestionType.MultipleChoice:
                        foreach (var answer in currentQuestion.Answers)
                        {
                            TextBox textbox = new TextBox
                            {
                                Text = answer.AnswerText,
                                AutoSize = true,
                                Height = 20,
                                Width = 600,
                                ReadOnly = true,
                                Location = new Point(10, currentY)
                            };

                            CheckBox checkBox = new CheckBox
                            {
                                Tag = answer,
                                Location = new Point(textbox.Right + 5, textbox.Top)
                            };

                            Test.Controls.Add(textbox);
                            Test.Controls.Add(checkBox);

                            currentY += textbox.Height + 10; // Обновляем позицию Y для следующего элемента
                        }

                        break;
                    case QuestionType.Sequence:
                        foreach (var answer in currentQuestion.Answers)
                        {
                            Label label = new Label
                            {
                                Text = answer.AnswerText,
                                AutoSize = true,
                                Location = new Point(10, currentY)
                            };

                            TextBox textbox = new TextBox
                            {
                                Tag = answer,
                                Width = 100,
                                Location = new Point(label.Right + 5, label.Top)
                            };
                            Test.Controls.Add(label);
                            Test.Controls.Add(textBox);
                            currentY += label.Height + 10; // Обновляем позицию Y для следующего элемента
                        }
                        break;
                    default:
                        MessageBox.Show("Неизвестный тип вопроса.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
                this.Controls.Add(questionPanel);
            }
        }

        private void InitializeButtons()
        {
            saveButton = new Button
            {
                Text = "Сохранить ответ",
                Location = new Point(10, 250),
                Size = new Size(120, 30),
                TextAlign = ContentAlignment.TopCenter
            };
            saveButton.Click += SaveButton_Click;
            Test.Controls.Add(saveButton);
            nextButton = new Button
            {
                Text = "Следующий вопрос",
                Location = new Point(140, 250),
                Size = new Size(120, 30),
                TextAlign = ContentAlignment.TopCenter
            };
            nextButton.Click += NextButton_Click;
            Test.Controls.Add(nextButton);
            previousButton = new Button
            {
                Text = "Предыдущий вопрос",
                Location = new Point(270, 250),
                Size = new Size(120, 30),
                TextAlign = ContentAlignment.TopCenter
            };
            previousButton.Click += PreviousButton_Click;
            Test.Controls.Add(previousButton);
            finishButton = new Button
            {
                Text = "Завершить тест",
                Location = new Point(400, 250),
                Size = new Size(120, 30),
                Visible = false, // Скрываем кнопку до завершения теста
                TextAlign = ContentAlignment.TopCenter
            };
            finishButton.Click += FinishButton_Click;
            Test.Controls.Add(finishButton);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (currentQuestionIndex < questions.Count)
            {
                var currentQuestion = questions[currentQuestionIndex];
                bool isCorrect = false;

                // Проверяем выбранный ответ в зависимости от типа вопроса
                switch (currentQuestion.QuestionType)
                {
                    case QuestionType.SingleChoice:
                        foreach (RadioButton radioButton in Test.Controls.OfType<RadioButton>())
                        {
                            if (radioButton.Checked)
                            {
                                var selectedAnswer = (Answer)radioButton.Tag;
                                isCorrect = selectedAnswer.IsCorrect;
                                break;
                            }
                        }
                        break;

                    case QuestionType.MultipleChoice:
                        isCorrect = true; // Предполагаем, что все выбранные ответы должны быть правильными
                        foreach (CheckBox checkBox in Test.Controls.OfType<CheckBox>())
                        {
                            var answer = (Answer)checkBox.Tag;
                            if (checkBox.Checked && !answer.IsCorrect)
                            {
                                isCorrect = false;
                                break;
                            }
                            else if (!checkBox.Checked && answer.IsCorrect)
                            {
                                isCorrect = false; // Если не выбран правильный ответ
                                break;
                            }
                        }
                        break;

                    case QuestionType.Sequence:
                        // Здесь должна быть логика для проверки порядка ответов
                        // Предположим, что у нас есть текстовые поля для ввода последовательности
                        foreach (TextBox textBox in Test.Controls.OfType<TextBox>())
                        {
                            var answer = (Answer)textBox.Tag;
                            if (textBox.Text.Equals(answer.AnswerText, StringComparison.OrdinalIgnoreCase))
                            {
                                isCorrect = true; // Если последовательность верна
                            }
                            else
                            {
                                isCorrect = false; // Если последовательность неверна
                                break;
                            }
                        }
                        break;
                }

                if (isCorrect)
                {
                    score++; // Увеличиваем счет, если ответ правильный
                    MessageBox.Show("Ответ правильный!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Ответ неправильный!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Переход к следующему вопросу
                currentQuestionIndex++;
                if (currentQuestionIndex < questions.Count)
                {
                    DisplayCurrentQuestion(); // Отображаем следующий вопрос
                }
                else
                {
                    MessageBox.Show("Вы ответили на все вопросы.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (currentQuestionIndex < questions.Count - 1)
            {
                currentQuestionIndex++;
                DisplayCurrentQuestion();
            }
            else
            {
                finishButton.Visible = true; // Показываем кнопку завершения теста, если это последний вопрос
                MessageBox.Show("Это последний вопрос.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            if (currentQuestionIndex > 0)
            {
                currentQuestionIndex--;
                DisplayCurrentQuestion();
            }
        }

        private void FinishButton_Click(object sender, EventArgs e)
        {
            // Выводим результаты теста
            MessageBox.Show($"Тест завершен! Вы набрали {score} из {totalQuestions} баллов.", "Результаты", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Сохраняем результаты в базу данных
            SaveTestResults(score, _testId); // Передаем testId

            // Закрытие формы или переход к следующему экрану
            this.Close(); // Или другой метод для перехода на следующий экран
        }

        [Obsolete]
        private void SaveTestResults(int score, int testId)
        {
            using (DB db = new DB())
            {
                try
                {
                    db.OpenConnection();

                    string query = "INSERT INTO TestResults (UserID, TestID, ResultTest) VALUES (@UserID, @TestID, @ResultTest)";
                    SqlCommand command = new SqlCommand(query, db.GetConnection());

                    command.Parameters.AddWithValue("@UserID", userId); // Используем ID пользователя
                    command.Parameters.AddWithValue("@TestID", testId);
                    command.Parameters.AddWithValue("@ResultTest", score);

                    command.ExecuteNonQuery();
                    db.CloseConnection();
                }
                catch (SqlException sqlEx)
                {
                    MessageBox.Show($"Ошибка базы данных: {sqlEx.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении результатов: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        // Метод для получения выбранного ответа (пример)
        [Obsolete]
        private List<Answer> GetAnswersForQuestion(int questionId)
        {
            List<Answer> answers = new List<Answer>();

            using (DB db = new DB())
            {
                string query = "SELECT * FROM Answers WHERE QuestionID = @QuestionID";
                SqlCommand command = new SqlCommand(query, db.GetConnection());
                command.Parameters.AddWithValue("@QuestionID", questionId);

                db.OpenConnection();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        answers.Add(new Answer
                        {
                            AnswerID = reader.GetInt32(0),
                            QuestionID = reader.GetInt32(1),
                            AnswerText = reader.GetString(2),
                            IsCorrect = reader.GetBoolean(3)
                        });
                    }
                }

                db.CloseConnection();
            }

            return answers;
        }

        private void ReturnButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Вы точно хотите закончить прохождение теста?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                this.Hide();
                userForm userForm = new userForm(userId);
                userForm.Show();
            }
        }
    }
}
