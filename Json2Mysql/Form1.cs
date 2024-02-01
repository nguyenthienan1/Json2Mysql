using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
            if (textBox1.TextLength == 0 || richTextBox1.TextLength == 0 || dataSource == null || dataSource.Count == 0)
            {
                MessageBox.Show("Please input table name and paste JSON text.");
                return;
            }
            string sql = JsonToMysql(textBox1.Text);

            richTextBox2.Clear();
            richTextBox2.Text = sql;
            label6.Text = "Rows count: " + rows.Count;
            UpdateDataGridView();
        }

        private JArray dataSource;
        private List<string> columns;
        private List<List<JToken>> rows;

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            InitializeDataSource();
            InitializeCheckedListBox();
        }

        private void InitializeDataSource()
        {
            if (dataSource != null)
                dataSource.Clear();
            try
            {
                dataSource = JArray.Parse(richTextBox1.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't deserialize json, please check again. " + ex.Message);
                return;
            }
        }

        private void InitializeCheckedListBox()
        {
            if (dataSource == null || dataSource.Count == 0)
            {
                return;
            }
            checkedListBox1.Items.Clear();
            List<string> columns = GetKeysInFirstObject(dataSource);
            checkedListBox1.Items.AddRange(columns.ToArray());
            for (int i = 0; i < columns.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        string JsonToMysql(string TableName)
        {
            //columns data follow checked list box column
            columns = new List<string>();
            List<string> columnsRemove = new List<string>();
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                bool isChecked = checkedListBox1.GetItemChecked(i);
                if (isChecked)
                    columns.Add(checkedListBox1.Items[i].ToString());
                else
                    columnsRemove.Add(checkedListBox1.Items[i].ToString());
            }

            //data follow checked list box column
            JArray data = dataSource.DeepClone() as JArray;
            RemoveKeysFromJArray(data, columnsRemove);
            rows = GetAllValuesListFromJArray(data);

            StringBuilder stringSql = new StringBuilder();
            //Create table
            if (checkBox2.Checked)
            {
                stringSql.Append(CreateTableSQL(TableName, data));
            }
            //Insert data
            stringSql.Append(InsertDataSQL(TableName));
            return stringSql.ToString();
        }

        void UpdateDataGridView()
        {
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();
            foreach (string s in columns)
            {
                dataGridView1.Columns.Add(s, s);
            }

            foreach (List<JToken> row in rows)
            {
                List<string> listValue = new List<string>();
                foreach (JToken jToken in row)
                {
                    listValue.Add(jToken.ToString(Formatting.None));
                }
                dataGridView1.Rows.Add(listValue.ToArray());
            }
        }

        string CreateTableSQL(string TableName, JArray data)
        {
            List<string> strings = new List<string>();
            List<KeyValuePair<string, JToken>> keyValuePairs = GetAllKeyValuePairs(data.First as JObject);
            foreach (KeyValuePair<string, JToken> keyValuePair in keyValuePairs)
            {
                strings.Add($"\t`{keyValuePair.Key}` {ToMySqlType(keyValuePair.Value.Type)}");
            }
            string stringSql = $"CREATE TABLE `{TableName}` (\n"
                + string.Join(",\n", strings)
                + "\n);\n\n";
            return stringSql;
        }

        string InsertDataSQL(string TableName)
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

            List<string> listStrColumns = new List<string>();
            foreach (string column in columns)
            {
                listStrColumns.Add($"`{column}`");
            }
            stringSql.Append(string.Join(", ", listStrColumns));
            stringSql.Append(")").Append(" VALUES ");

            List<string> listStrRows = new List<string>();
            foreach (List<JToken> row in rows)
            {
                List<string> listStrRow = new List<string>();
                foreach (JToken jToken in row)
                {
                    if (jToken.Type == JTokenType.String)
                    {
                        listStrRow.Add($"'{jToken}'");
                    }
                    else if (jToken.Type == JTokenType.Array || jToken.Type == JTokenType.Object)
                    {
                        listStrRow.Add($"'{jToken.ToString(Formatting.None)}'");
                    }
                    else
                    {
                        listStrRow.Add(jToken.ToString(Formatting.None));
                    }
                }
                listStrRows.Add("\n(" + string.Join(", ", listStrRow) + ")");
            }
            stringSql.Append(string.Join(",", listStrRows));
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

        static List<List<JToken>> GetAllValuesListFromJArray(JArray jsonArray)
        {
            List<List<JToken>> allValuesList = new List<List<JToken>>();

            foreach (JObject obj in jsonArray.Children<JObject>())
            {
                List<JToken> values = new List<JToken>();

                foreach (JProperty property in obj.Properties())
                {
                    values.Add(property.Value);
                }

                allValuesList.Add(values);
            }

            return allValuesList;
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
