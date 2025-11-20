using System;
using System.Data;
using System.Windows.Forms;

namespace WinformMayTinh
{
    public partial class Form1 : Form
    {
        private bool lastWasOperator = false;
        private bool showingResult = false;

        public Form1()
        {
            InitializeComponent();
            WireEvents();
            InitializeState();
        }

        private void InitializeState()
        {
            if (string.IsNullOrWhiteSpace(txtKq.Text))
                txtKq.Text = "0";
            txtKq.ReadOnly = true;
            AcceptButton = btnKq; // Enter triggers "="
        }

        private void WireEvents()
        {
            // Digits
            btn0.Click += Digit_Click;
            btn1.Click += Digit_Click;
            btn2.Click += Digit_Click;
            btn3.Click += Digit_Click;
            btn4.Click += Digit_Click;
            btn5.Click += Digit_Click;
            btn6.Click += Digit_Click;
            btn7.Click += Digit_Click;
            btn8.Click += Digit_Click;
            btn9.Click += Digit_Click;

            // Decimal point
            btnCham.Click += Decimal_Click;

            // Operators
            btnCong.Click += Operator_Click;
            btnTru.Click += Operator_Click;
            btnNhan.Click += Operator_Click;
            btnChia.Click += Operator_Click;

            // Equals
            btnKq.Click += Equals_Click;

            // Delete (backspace)
            btxDelete.Click += Delete_Click;

            // Optional keyboard support
            KeyPreview = true;
            KeyPress += Form1_KeyPress;
        }

        // Digit button handler
        private void Digit_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var digit = btn.Text;

            if (showingResult)
            {
                txtKq.Text = digit;
                showingResult = false;
                lastWasOperator = false;
                return;
            }

            if (txtKq.Text == "0" && digit != "0")
            {
                txtKq.Text = digit;
            }
            else
            {
                txtKq.Text += digit;
            }

            lastWasOperator = false;
        }

        // Decimal point handler
        private void Decimal_Click(object sender, EventArgs e)
        {
            if (showingResult)
            {
                txtKq.Text = "0.";
                showingResult = false;
                lastWasOperator = false;
                return;
            }

            var currentNumber = GetCurrentNumberToken();
            if (currentNumber.IndexOf(".", StringComparison.Ordinal) >= 0)
                return; // already has decimal

            if (lastWasOperator || txtKq.Text.Length == 0)
                txtKq.Text += "0.";
            else
                txtKq.Text += ".";

            lastWasOperator = false;
        }

        // Operator handler
        private void Operator_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            string op = btn.Text; // + - * /

            if (showingResult)
            {
                showingResult = false;
            }

            if (txtKq.Text.Length == 0)
            {
                if (op == "-")
                {
                    txtKq.Text = "-";
                    lastWasOperator = false;
                }
                return;
            }

            char lastChar = txtKq.Text[txtKq.Text.Length - 1];
            if (IsOperator(lastChar))
            {
                // replace previous operator
                txtKq.Text = txtKq.Text.Substring(0, txtKq.Text.Length - 1) + op;
            }
            else
            {
                txtKq.Text += op;
            }

            lastWasOperator = true;
        }

        // Equals handler
        private void Equals_Click(object sender, EventArgs e)
        {
            try
            {
                string expr = txtKq.Text;
                if (string.IsNullOrWhiteSpace(expr)) return;

                // Remove trailing operator(s)
                while (expr.Length > 0 && IsOperator(expr[expr.Length - 1]))
                    expr = expr.Substring(0, expr.Length - 1);

                if (expr.Length == 0) return;

                object raw = new DataTable().Compute(expr, null);
                string result = FormatResult(raw);
                txtKq.Text = result;
                showingResult = true;
                lastWasOperator = false;
            }
            catch
            {
                txtKq.Text = "Error";
                showingResult = true;
                lastWasOperator = false;
            }
        }

        // Delete (backspace) handler
        private void Delete_Click(object sender, EventArgs e)
        {
            if (showingResult)
            {
                txtKq.Text = "0";
                showingResult = false;
                lastWasOperator = false;
                return;
            }

            if (string.IsNullOrEmpty(txtKq.Text) || txtKq.Text == "0" || txtKq.Text == "Error")
            {
                txtKq.Text = "0";
                lastWasOperator = false;
                return;
            }

            txtKq.Text = txtKq.Text.Substring(0, txtKq.Text.Length - 1);
            if (txtKq.Text.Length == 0)
            {
                txtKq.Text = "0";
                lastWasOperator = false;
                return;
            }

            lastWasOperator = IsOperator(txtKq.Text[txtKq.Text.Length - 1]);
        }

        // Keyboard support
        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            char c = e.KeyChar;

            if (char.IsDigit(c))
            {
                Digit_Click(new Button { Text = c.ToString() }, EventArgs.Empty);
                e.Handled = true;
            }
            else if (c == '.' || c == ',')
            {
                Decimal_Click(btnCham, EventArgs.Empty);
                e.Handled = true;
            }
            else if ("+-*/".IndexOf(c) >= 0)
            {
                Operator_Click(new Button { Text = c.ToString() }, EventArgs.Empty);
                e.Handled = true;
            }
            else if (c == '=' || c == '\r')
            {
                Equals_Click(btnKq, EventArgs.Empty);
                e.Handled = true;
            }
            else if (c == '\b')
            {
                Delete_Click(btxDelete, EventArgs.Empty);
                e.Handled = true;
            }
        }

        // Helper: is operator
        private static bool IsOperator(char ch)
        {
            return ch == '+' || ch == '-' || ch == '*' || ch == '/';
        }

        // Helper: get current number token after last operator
        private string GetCurrentNumberToken()
        {
            string s = txtKq.Text;
            if (string.IsNullOrEmpty(s)) return "";
            int i = s.Length - 1;
            while (i >= 0 && !IsOperator(s[i]))
                i--;
            return s.Substring(i + 1);
        }

        // Helper: format result removing trailing .0
        private string FormatResult(object raw)
        {
            if (raw == null) return "0";
            double d;
            if (double.TryParse(Convert.ToString(raw), out d))
            {
                if (Math.Abs(d % 1) < 1e-12)
                    return ((long)Math.Round(d)).ToString();
                return d.ToString();
            }
            return raw.ToString();
        }

    
    }
}