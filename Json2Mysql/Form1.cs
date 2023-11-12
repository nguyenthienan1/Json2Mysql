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
            string sql = JsonToMysql(richTextBox1.Text, textBox1.Text);
            if (sql != null)
            {
                richTextBox2.Clear();
                richTextBox2.Text = sql;
            }
        }

        string JsonToMysql(string JsonText, string TableName)
        {
            JArray rows = null;
            try
            {
                rows = JsonConvert.DeserializeObject<JArray>(JsonText);
            }
            catch (Exception e)
            {
                MessageBox.Show("Can't deserialize json, please check again. " + e.Message);
                return null;
            }

            StringBuilder stringSql = new StringBuilder();
            if (checkBox1.Checked)
            {
                stringSql.Append("INSERT IGNORE INTO " + TableName + " (");
            } else
            {
                stringSql.Append("INSERT INTO " + TableName + " (");
            }
            

            JObject firstRow = (JObject)rows.First;
            List<KeyValuePair<string, JToken>> listFirstRow = ToList(firstRow.GetEnumerator());
            for (int i = 0; i < listFirstRow.Count; i++)
            {
                KeyValuePair<string, JToken> k = listFirstRow.ElementAt(i);
                stringSql.Append("`" + k.Key + "`");
                if (i < listFirstRow.Count - 1)
                {
                    stringSql.Append(", ");
                }
                else
                {
                    stringSql.Append(")");
                }
            }
            stringSql.Append(" VALUES ");

            foreach (JObject row in rows)
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
