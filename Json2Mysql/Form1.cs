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
            string sql = JsonToMysql(textBox1.Text);
            if (sql != null)
            {
                richTextBox2.Clear();
                richTextBox2.Text = sql;
            }
        }

        List<string> columns = new List<string>();
        JArray rows = null;

        /// <summary>
        /// Deserialize Json Object and create columns after change text in richtextbox1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            columns.Clear();
            if (rows != null)
                rows.Clear();
            checkedListBox1.Items.Clear();
            try
            {
                rows = JsonConvert.DeserializeObject<JArray>(richTextBox1.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't deserialize json, please check again. " + ex.Message);
                return;
            }
            if (rows == null || rows.Count == 0)
            {
                MessageBox.Show("Can't deserialize json, please check again.");
                return;
            }

            List<KeyValuePair<string, JToken>> listFieldFirstRow = ToList(((JObject)rows.First).GetEnumerator());

            foreach (KeyValuePair<string, JToken> f in listFieldFirstRow)
            {
                columns.Add(f.Key);
            }
            checkedListBox1.Items.AddRange(columns.ToArray());
            for (int i = 0; i < columns.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        string JsonToMysql(string TableName)
        {
            //columns data follow checked list box column
            List<string> columnsData = new List<string>();
            foreach (var i in checkedListBox1.CheckedItems)
            {
                columnsData.Add(i.ToString());
            }

            //rows data follow checked list box column
            JArray rowsData = new JArray();
            foreach (JObject row in rows)
            {
                JObject keyValuePairs = new JObject();
                List<KeyValuePair<string, JToken>> listJObject = ToList(row.GetEnumerator());
                foreach (KeyValuePair<string, JToken> jObject in listJObject)
                {
                    if (columnsData.Contains(jObject.Key))
                    {
                        keyValuePairs.Add(jObject.Key, jObject.Value);
                    }
                }
                rowsData.Add(keyValuePairs);
            }

            //Update data grid view
            UpdateDataGrid(columnsData, rowsData);

            StringBuilder stringSql = new StringBuilder();

            //Create table
            if (checkBox2.Checked)
            {
                stringSql.Append(CreateTableSQL(TableName, rowsData));
            }

            //Insert data
            stringSql.Append(InsertDataSQL(TableName, columnsData, rowsData));

            return stringSql.ToString();
        }

        void UpdateDataGrid(List<string> columnsData, JArray rowsData)
        {
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();
            foreach (string s in columnsData)
            {
                DataGridViewColumn column = new DataGridViewTextBoxColumn();
                column.Name = s;
                column.HeaderText = s;
                dataGridView1.Columns.Add(column);
            }

            foreach (JObject row in rows)
            {
                List<string> listValue = new List<string>();
                List<KeyValuePair<string, JToken>> listJObject = ToList(row.GetEnumerator());
                for (int i = 0; i < listJObject.Count; i++)
                {
                    KeyValuePair<string, JToken> jObject = listJObject.ElementAt(i);
                    if (columnsData.Contains(jObject.Key))
                    {
                        if (jObject.Value.Type == JTokenType.Array)
                        {
                            listValue.Add(jObject.Value.ToString(Formatting.None));
                        }
                        else
                        {
                            listValue.Add(jObject.Value.ToString());
                        }
                    }
                }
                dataGridView1.Rows.Add(listValue.ToArray());
            }
        }

        string CreateTableSQL(string TableName, JArray rowsData)
        {
            StringBuilder stringSql = new StringBuilder();
            stringSql.Append($"CREATE TABLE `{TableName}` (\n");

            List<KeyValuePair<string, JToken>> listFieldFirstRow = ToList(((JObject)rowsData.First).GetEnumerator());
            foreach (KeyValuePair<string, JToken> keyValue in listFieldFirstRow)
            {
                stringSql.Append($"\t`{keyValue.Key}` {ToMySqlType(keyValue.Value.Type)}");
                stringSql.Append(",\n");
            }
            stringSql.Remove(stringSql.Length - 2, 2); //remove last ",\n"
            stringSql.Append("\n);\n\n");
            return stringSql.ToString();
        }

        string InsertDataSQL(string TableName, List<string> columnsData, JArray rowsData)
        {
            label6.Text = "Rows count: " + rowsData.Count;
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

            foreach (string column in columnsData)
            {
                stringSql.Append($"`{column}`");
                stringSql.Append(", ");
            }
            stringSql.Remove(stringSql.Length - 2, 2); //remove last ", "
            stringSql.Append(")");
            stringSql.Append(" VALUES ");
            foreach (JObject row in rowsData)
            {
                stringSql.Append("\n(");
                List<KeyValuePair<string, JToken>> listJObject = ToList(row.GetEnumerator());

                foreach (KeyValuePair<string, JToken> keyValue in listJObject)
                {
                    if (keyValue.Value.Type == JTokenType.String)
                    {
                        stringSql.Append($"'{keyValue.Value}'");
                    }
                    else if (keyValue.Value.Type == JTokenType.Array)
                    {
                        stringSql.Append($"'{keyValue.Value.ToString(Formatting.None)}'");
                    }
                    else
                    {
                        stringSql.Append(keyValue.Value);
                    }

                    stringSql.Append(", ");
                }
                stringSql.Remove(stringSql.Length - 2, 2); //remove last ", "
                stringSql.Append("),");
            }

            stringSql = stringSql.Remove(stringSql.Length - 1, 1).Append(";");
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

        List<T> ToList<T>(IEnumerator<T> enumerator)
        {
            var list = new List<T>();
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }
            return list;
        }
    }
}
