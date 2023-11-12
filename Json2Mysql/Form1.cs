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
            List<KeyValuePair<string, JToken>> listFirstRow = ToList(((JObject)rows.First).GetEnumerator());

            foreach (KeyValuePair<string, JToken> k in listFirstRow)
            {
                columns.Add(k.Key);
            }
            checkedListBox1.Items.AddRange(columns.ToArray());
            for (int i = 0; i < columns.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        string JsonToMysql(string TableName)
        {
            JArray rowsData = new JArray();
            Console.WriteLine(rowsData.Count);

            List<string> columnData = new List<string>();
            foreach (var i in checkedListBox1.CheckedItems)
            {
                columnData.Add(i.ToString());
            }
            Console.WriteLine(columnData);

            foreach (JObject row in rows)
            {
                JObject keyValuePairs = new JObject();
                List<KeyValuePair<string, JToken>> listJToken = ToList(row.GetEnumerator());
                for (int i = 0; i < listJToken.Count; i++)
                {
                    KeyValuePair<string, JToken> kJToken = listJToken.ElementAt(i);
                    if (columnData.Contains(kJToken.Key))
                    {
                        keyValuePairs.Add(kJToken.Key, kJToken.Value);
                    }
                }
                rowsData.Add(keyValuePairs);
            }

            StringBuilder stringSql = new StringBuilder();
            if (checkBox1.Checked)
            {
                stringSql.Append("INSERT IGNORE INTO " + TableName + " (");
            } else
            {
                stringSql.Append("INSERT INTO " + TableName + " (");
            }

            for (int i = 0; i < columnData.Count; i++)
            {
                stringSql.Append("`" + columnData[i] + "`");
                if (i < columnData.Count - 1)
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
                List<KeyValuePair<string, JToken>> listJToken = ToList(row.GetEnumerator());

                for (int i = 0; i < listJToken.Count; i++)
                {
                    KeyValuePair<string, JToken> kJToken = listJToken.ElementAt(i);
                    if (kJToken.Value.Type == JTokenType.String || kJToken.Value.Type == JTokenType.Array)
                    {
                        stringSql.Append("'" + kJToken.Value.ToString(Formatting.None) + "'");
                    }
                    else
                    {
                        stringSql.Append(kJToken.Value);
                    }

                    if (i < listJToken.Count - 1)
                    {
                        stringSql.Append(", ");
                    }
                    else
                    {
                        stringSql.Append("),");
                    }
                }
            }
            string sql = stringSql.Remove(stringSql.Length - 1, 1).Append(";").ToString();
            return sql;
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
