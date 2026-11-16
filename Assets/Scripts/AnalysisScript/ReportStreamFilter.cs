using System;
using System.Collections.Generic;
using System.Text;

namespace Promotion.Analysis
{
    public class ReportStreamFilter
    // 서버에서 조금씩 오는 글을 검사해 화면에 쓸 글로 바꾸는 클래스
    // -> "- [F1] ..." 줄의 근거 번호가 실제 분석 항목에 없으면 그 줄을 통째로 버림 (없는 사실이 표시되지 않게 함)
    // -> 폰트에 없는 글자와 태그 기호를 지움. 서식이 정해진 머리글은 금색 제목으로 바꿈
    // -> 끝 표시([END])가 왔는지 기억함
    {
        const string EndMark = "[END]";

        readonly HashSet<string> _validIds;
        readonly Func<char, bool> _canDraw;
        readonly Action<string> _emit;
        readonly Action<string> _onItem;    // 근거 번호가 확인된 항목 줄이 시작될 때 그 번호를 알림 (체스판 장면 연동)
        readonly StringBuilder _line = new();   // 아직 판단하지 못한 줄의 앞부분
        bool _passing;      // 지금 줄을 그대로 내보내는 중인지
        bool _dropping;     // 지금 줄을 버리는 중인지
        bool _skipSpaces;   // 항목 표시(·) 뒤의 공백을 건너뛰는 중인지

        public bool Completed { get; private set; }
        public bool HasOutput { get; private set; }
        public int DroppedLines { get; private set; }

        public ReportStreamFilter(IEnumerable<string> validIds, Func<char, bool> canDraw, Action<string> emit, Action<string> onItem = null)
        {
            _onItem = onItem;
            _validIds = new HashSet<string>(validIds);
            _canDraw = canDraw;
            _emit = emit;
        }

        public void Push(string chunk)
        {
            foreach (char c in chunk)
                PushChar(c);
        }

        public void Finish()    // 스트림이 끝났을 때 남은 줄을 처리하는 함수
        {
            if (_line.Length > 0)
                EndLine();
        }

        void PushChar(char c)
        {
            if (c == '\r')
                return;
            if (c == '\n')
            {
                bool wasPassing = _passing;
                if (_passing || _dropping)
                {
                    _passing = false;
                    _dropping = false;
                    _skipSpaces = false;
                    if (wasPassing)
                        Write("\n");
                    return;
                }
                EndLine();
                return;
            }

            if (_dropping)
                return;
            if (_passing)
            {
                if (_skipSpaces && c == ' ')
                    return;
                _skipSpaces = false;
                WriteClean(c);
                return;
            }

            _line.Append(c);
            Decide();
        }

        void Decide()   // 줄 앞부분만 보고 어떻게 처리할지 정하는 함수 -> 아직 알 수 없으면 더 기다림
        {
            string head = _line.ToString();

            if (head[0] == '#' || EndMark.StartsWith(head, StringComparison.Ordinal))   // 머리글이나 끝 표시일 수 있으면 줄 끝까지 기다림
                return;

            if (head[0] != '-')     // 일반 문장은 바로 내보냄
            {
                BeginPassing(head);
                return;
            }

            // "- [F1] " 형식의 항목 줄
            if (head.Length < 3)
            {
                if (head.Length == 2 && head[1] != ' ')
                    Drop();
                return;
            }
            if (head[2] != '[')
            {
                Drop();
                return;
            }

            int close = head.IndexOf(']');
            if (close < 0)
            {
                if (head.Length > 12)   // 근거 번호가 이렇게 길 수는 없음
                    Drop();
                return;
            }

            if (!_validIds.Contains(head.Substring(3, close - 3)))
            {
                Drop();
                return;
            }

            _onItem?.Invoke(head.Substring(3, close - 3));
            BeginPassing("· " + head.Substring(close + 1).TrimStart());
            _skipSpaces = head.Substring(close + 1).Trim().Length == 0;   // 아직 본문이 오지 않았다면 이어서 오는 공백을 건너뜀
        }

        void BeginPassing(string start)
        {
            _line.Clear();
            _passing = true;
            foreach (char c in start)
                WriteClean(c);
        }

        void Drop()
        {
            _line.Clear();
            _dropping = true;
            DroppedLines++;
        }

        void EndLine()  // 줄 끝까지 기다렸던 줄(머리글, 끝 표시, 미완성 항목)을 처리하는 함수
        {
            string line = _line.ToString().Trim();
            _line.Clear();

            if (line == EndMark)
            {
                Completed = true;
                return;
            }
            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                string title = Clean(line.TrimStart('#').Trim());
                if (HasOutput)
                    Write("\n");
                Write("<b><color=#F2BC51>" + title + "</color></b>\n");
                return;
            }
            if (line.Length > 0 && line[0] == '-')  // 근거 번호를 끝내 확인하지 못한 항목
            {
                DroppedLines++;
                return;
            }
            if (line.Length > 0)
                Write(Clean(line) + "\n");
        }

        void Write(string text)
        {
            HasOutput = true;
            _emit(text);
        }

        void WriteClean(char c)
        {
            if (Allowed(c))
                Write(c.ToString());
        }

        bool Allowed(char c)
        {
            return c != '<' && c != '>' && _canDraw(c);
        }

        string Clean(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (Allowed(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
