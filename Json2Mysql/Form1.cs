using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Json2Mysql
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.TextLength == 0)
            {
                MessageBox.Show("Please input table name!");
                return;
            }
            if (richTextBox1.TextLength == 0)
            {
                MessageBox.Show("Please paste json text!");
                return;
            }
            if (rowsSource == null || rowsSource.Count == 0)
            {
                return;
            }
            string sql = JsonToMysql(textBox1.Text);
            if (sql != null)
            {
                richTextBox2.Clear();
                richTextBox2.Text = sql;
            }
        }

        JArray rowsSource = null;

        /// <summary>
        /// Deserialize Json Object and create columns after change text in richtextbox1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (rowsSource != null)
                rowsSource.Clear();
            checkedListBox1.Items.Clear();
            try
            {
                rowsSource = JArray.Parse(richTextBox1.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't deserialize json, please check again. " + ex.Message);
                return;
            }
            if (rowsSource == null || rowsSource.Count == 0)
            {
                MessageBox.Show("Can't deserialize json, please check again.");
                return;
            }

            List<string> columns = GetKeysInFirstObject(rowsSource);
            checkedListBox1.Items.AddRange(columns.ToArray());
            for (int i = 0; i < columns.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        string JsonToMysql(string TableName)
        {
            //columns data follow checked list box column
            List<string> columns = new List<string>();
            List<string> columnsRemove = new List<string>();
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                bool isChecked = checkedListBox1.GetItemChecked(i);
                if (isChecked)
                    columns.Add(checkedListBox1.Items[i].ToString());
                else
                    columnsRemove.Add(checkedListBox1.Items[i].ToString());
            }

            //rows data follow checked list box column
            JArray rows = rowsSource.DeepClone() as JArray;
            RemoveKeysFromJArray(rows, columnsRemove);

            //Update data grid view
            UpdateDataGrid(columns, rows);

            StringBuilder stringSql = new StringBuilder();

            //Create table
            if (checkBox2.Checked)
            {
                stringSql.Append(CreateTableSQL(TableName, rows));
            }

            //Insert data
            stringSql.Append(InsertDataSQL(TableName, columns, rows));

            return stringSql.ToString();
        }

        void UpdateDataGrid(List<string> columns, JArray rows)
        {
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();
            foreach (string s in columns)
            {
                dataGridView1.Columns.Add(s, s);
            }

            foreach (JObject row in rows)
            {
                List<string> listValue = new List<string>();
                List<KeyValuePair<string, JToken>> keyValuePairs = GetAllKeyValuePairs(row);
                foreach (KeyValuePair<string, JToken> keyValuePair in keyValuePairs)
                {
                    if (keyValuePair.Value.Type == JTokenType.Array)
                    {
                        listValue.Add(keyValuePair.Value.ToString(Formatting.None));
                    }
                    else
                    {
                        listValue.Add(keyValuePair.Value.ToString());
                    }
                }
                dataGridView1.Rows.Add(listValue.ToArray());
            }
        }

        string CreateTableSQL(string TableName, JArray rows)
        {
            StringBuilder stringSql = new StringBuilder();
            stringSql.Append($"CREATE TABLE `{TableName}` (\n");

            List<KeyValuePair<string, JToken>> keyValuePairs = GetAllKeyValuePairs(rows.First as JObject);
            foreach (KeyValuePair<string, JToken> keyValuePair in keyValuePairs)
            {
                stringSql.Append($"\t`{keyValuePair.Key}` {ToMySqlType(keyValuePair.Value.Type)}");
                stringSql.Append(",\n");
            }
            stringSql.Remove(stringSql.Length - 2, 2); //remove last ",\n"
            stringSql.Append("\n);\n\n");
            return stringSql.ToString();
        }

        string InsertDataSQL(string TableName, List<string> columns, JArray rows)
        {
            label6.Text = "Rows count: " + rows.Count;
            StringBuilder stringSql = new StringBuilder();

            //Insert ignore
            if (checkBox1.Checked)
            {
                stringSql.Append($"INSERT IGNORE INTO `{TableName}` (");
            }
            else
            {
                stringSql.Append($"INSERT INTO `{TableName}` (");
            }

            foreach (string column in columns)
            {
                stringSql.Append($"`{column}`");
                stringSql.Append(", ");
            }
            stringSql.Remove(stringSql.Length - 2, 2); //remove last ", "
            stringSql.Append(")");
            stringSql.Append(" VALUES ");
            foreach (JObject row in rows)
            {
                stringSql.Append("\n(");
                List<KeyValuePair<string, JToken>> keyValuePairs = GetAllKeyValuePairs(row);

                foreach (KeyValuePair<string, JToken> keyValuePair in keyValuePairs)
                {
                    if (keyValuePair.Value.Type == JTokenType.String)
                    {
                        stringSql.Append($"'{keyValuePair.Value}'");
                    }
                    else if (keyValuePair.Value.Type == JTokenType.Array)
                    {
                        stringSql.Append($"'{keyValuePair.Value.ToString(Formatting.None)}'");
                    }
                    else
                    {
                        stringSql.Append(keyValuePair.Value);
                    }

                    stringSql.Append(", ");
                }
                stringSql.Remove(stringSql.Length - 2, 2); //remove last ", "
                stringSql.Append("),");
            }

            stringSql.Remove(stringSql.Length - 1, 1); //remove ","
            stringSql.Append(";");
            return stringSql.ToString();
        }

        string ToMySqlType(JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Integer:
                    return "INT";
                case JTokenType.Float:
                    return "DOUBLE";
                default:
                    return "TEXT";
            }
        }

        static List<string> GetKeysInFirstObject(JArray jsonArray)
        {
            List<string> keys = new List<string>();

            if (jsonArray.Count > 0 && jsonArray.First is JObject firstObject)
            {
                foreach (JProperty property in firstObject.Properties())
                {
                    keys.Add(property.Name);
                }
            }

            return keys;
        }

        static List<KeyValuePair<string, JToken>> GetAllKeyValuePairs(JObject jsonObject)
        {
            List<KeyValuePair<string, JToken>> keyValuePairs = new List<KeyValuePair<string, JToken>>();

            foreach (JProperty property in jsonObject.Properties())
            {
                keyValuePairs.Add(new KeyValuePair<string, JToken>(property.Name, property.Value));
            }

            return keyValuePairs;
        }

        static void RemoveKeysFromJArray(JArray jsonArray, List<string> keysToRemove)
        {
            if (keysToRemove.Count == 0)
                return;
            foreach (JObject obj in jsonArray.Children<JObject>())
            {
                foreach (string key in keysToRemove)
                {
                    JProperty propertyToRemove = obj.Property(key);
                    propertyToRemove?.Remove();
                }
            }
        }
    }
}
