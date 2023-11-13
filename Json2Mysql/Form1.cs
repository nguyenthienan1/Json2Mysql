using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                for (int i = 0; i < listJObject.Count; i++)
                {
                    KeyValuePair<string, JToken> jObject = listJObject.ElementAt(i);
                    if (columnsData.Contains(jObject.Key))
                    {
                        keyValuePairs.Add(jObject.Key, jObject.Value);
                    }
                }
                rowsData.Add(keyValuePairs);
            }

            StringBuilder stringSql = new StringBuilder();

            //Create table
            if (checkBox2.Checked)
            {
                stringSql.Append(createTableSQL(TableName, rowsData));
            }

            //Insert data
            stringSql.Append(insertDataSQL(TableName, columnsData, rowsData));

            return stringSql.ToString();
        }

        string createTableSQL(string TableName, JArray rowsData)
        {
            StringBuilder stringSql = new StringBuilder();
            stringSql.Append($"CREATE TABLE `{TableName}` (\n");

            List<KeyValuePair<string, JToken>> listFieldFirstRow = ToList(((JObject)rowsData.First).GetEnumerator());
            for (int i = 0; i < listFieldFirstRow.Count; i++)
            {
                string key = listFieldFirstRow[i].Key;
                JToken value = listFieldFirstRow[i].Value;
                stringSql.Append($"\t`{key}` {toTypeSQL(value.Type)}");
                if (i < listFieldFirstRow.Count - 1)
                {
                    stringSql.Append(",\n");
                }
                else
                {
                    stringSql.Append("\n);\n\n");
                }
            }
            return stringSql.ToString();
        }

        string insertDataSQL(string TableName, List<string> columnDatas, JArray rowsData)
        {
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

            for (int i = 0; i < columnDatas.Count; i++)
            {
                stringSql.Append($"`{columnDatas[i]}`");
                if (i < columnDatas.Count - 1)
                {
                    stringSql.Append(", ");
                }
                else
                {
                    stringSql.Append(")");
                }
            }
            stringSql.Append(" VALUES ");

            foreach (JObject row in rowsData)
            {
                stringSql.Append("\n(");
                List<KeyValuePair<string, JToken>> listJObject = ToList(row.GetEnumerator());

                for (int i = 0; i < listJObject.Count; i++)
                {
                    KeyValuePair<string, JToken> jObject = listJObject.ElementAt(i);
                    if (jObject.Value.Type == JTokenType.String || jObject.Value.Type == JTokenType.Array)
                    {
                        stringSql.Append("'" + jObject.Value.ToString(Formatting.None) + "'");
                    }
                    else
                    {
                        stringSql.Append(jObject.Value);
                    }

                    if (i < listJObject.Count - 1)
                    {
                        stringSql.Append(", ");
                    }
                    else
                    {
                        stringSql.Append("),");
                    }
                }
            }

            stringSql = stringSql.Remove(stringSql.Length - 1, 1).Append(";");
            return stringSql.ToString();
        }

        string toTypeSQL(JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Integer:
                    return "INT";
                case JTokenType.Float:
                    return "DOUBLE";
                default:
                    return "VARCHAR(1024)";
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
