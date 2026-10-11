using System.Collections.Generic;
using System.Text;
using Promotion.Core;
using Promotion.Data;
using UnityEditor;
using UnityEngine;

namespace Promotion.EditorTools
{
    /// <summary>
    /// 정답이 알려진 국면들로 체스 규칙 엔진을 검사한다. 시작 국면의 수 개수(perft),
    /// 체크메이트, 스테일메이트, 핀, 프로모션을 확인한다. ChessRules, SearchBoard,
    /// MoveResolver나 기물의 행마 데이터를 수정한 뒤에는 Promotion > Tests > Chess Rules
    /// 메뉴로 실행한다.
    /// </summary>
    public static class ChessRulesSelfTest
    {
        const string PieceFolder = "Assets/Item/ChessPieces/PieceDefinitions/";

        [MenuItem("Promotion/Tests/Chess Rules")]
        public static void RunFromMenu() => Run(4);

        /// <returns>모든 검사를 통과하면 true다.</returns>
        public static bool Run(int perftDepth)
        {
            var log = new StringBuilder("[ChessRulesSelfTest]\n");
            int failures = 0;

            void Check(string name, bool ok, string detail = "")
            {
                if (!ok) failures++;
                log.AppendLine((ok ? "  PASS " : "  FAIL ") + name + (detail.Length > 0 ? "  (" + detail + ")" : ""));
            }

            var set = PieceSet.Load();
            if (set == null)
            {
                Debug.LogError("[ChessRulesSelfTest] Could not load the piece definitions from " + PieceFolder);
                return false;
            }

            // 1. 시작 국면에서의 수 개수.
            long[] expected = { 1, 20, 400, 8902, 197281 };
            var opening = set.OpeningPosition();
            var before = Describe(opening);
            for (int d = 1; d <= perftDepth && d < expected.Length; d++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                long n = ChessRules.Perft(opening, Team.Player, d, set.queen);
                Check("perft " + d, n == expected[d], n + " moves, " + watch.ElapsedMilliseconds + " ms");
            }
            Check("make/unmake leaves the board untouched", Describe(opening) == before);

            // 2. 바보 메이트(Fool's mate): 1. f3 e5 2. g4 Qh4# — 백이 메이트당한다.
            var fool = set.OpeningPosition();
            Play(fool, 5, 1, 5, 2);
            Play(fool, 4, 6, 4, 4);
            Play(fool, 6, 1, 6, 3);
            Play(fool, 3, 7, 7, 3);
            Check("fool's mate is checkmate", ChessRules.GetStatus(fool, Team.Player) == ChessStatus.Checkmate);

            // 3. 같은 게임도 한 수 전에는 체크가 없고 계속 둘 수 있는 상태다.
            var notYet = set.OpeningPosition();
            Play(notYet, 5, 1, 5, 2);
            Play(notYet, 4, 6, 4, 4);
            Check("ordinary position is ongoing", ChessRules.GetStatus(notYet, Team.Player) == ChessStatus.Ongoing);

            // 4. 스테일메이트: 흑 킹 a8, 백 퀸 b6, 백 킹 h1, 흑 차례.
            var stale = new SearchBoard(8, 8);
            stale.Add(set.king, Team.Enemy, new Coord(0, 7));
            stale.Add(set.queen, Team.Player, new Coord(1, 5));
            stale.Add(set.king, Team.Player, new Coord(7, 0));
            Check("king-and-queen stalemate", ChessRules.GetStatus(stale, Team.Enemy) == ChessStatus.Stalemate);

            // 5. 피할 수 있는 체크는 메이트가 아니라 Check다.
            var check = new SearchBoard(8, 8);
            check.Add(set.king, Team.Enemy, new Coord(4, 7));
            check.Add(set.rook, Team.Player, new Coord(4, 0));
            check.Add(set.king, Team.Player, new Coord(0, 0));
            Check("rook check with escape squares", ChessRules.GetStatus(check, Team.Enemy) == ChessStatus.Check);

            // 6. 핀에 걸린 나이트는 전혀 움직일 수 없다.
            var pin = new SearchBoard(8, 8);
            pin.Add(set.king, Team.Player, new Coord(4, 0));
            var knight = pin.Add(set.knight, Team.Player, new Coord(4, 1));
            pin.Add(set.rook, Team.Enemy, new Coord(4, 7));
            pin.Add(set.king, Team.Enemy, new Coord(0, 7));
            var moves = new List<MoveOption>();
            ChessRules.GetLegalMoves(pin, knight, moves);
            Check("pinned knight has no moves", moves.Count == 0, moves.Count + " found");

            // 7. 킹은 공격받는 칸으로 들어갈 수 없다.
            var guard = new SearchBoard(8, 8);
            var whiteKing = guard.Add(set.king, Team.Player, new Coord(4, 0));
            guard.Add(set.rook, Team.Enemy, new Coord(0, 1));
            guard.Add(set.king, Team.Enemy, new Coord(7, 7));
            ChessRules.GetLegalMoves(guard, whiteKing, moves);
            bool stepsIntoRank = moves.Exists(m => m.target.y == 1);
            Check("king avoids the rook's rank", !stepsIntoRank && moves.Count == 2, moves.Count + " moves");

            // 8. 프로모션: 7랭크의 백 폰이 8랭크에서 승급하고, 수를 무르면 폰으로 복원된다.
            var promo = new SearchBoard(8, 8);
            var pawn = promo.Add(set.pawn, Team.Player, new Coord(0, 6), hasMoved: true);
            promo.Add(set.king, Team.Player, new Coord(7, 0));
            promo.Add(set.king, Team.Enemy, new Coord(7, 7));
            bool isPromo = ChessRules.IsPromotion(promo, pawn, new Coord(0, 7));
            var undo = promo.Make(pawn, new Coord(0, 7), set.queen);
            bool becameQueen = promo.GetPiece(new Coord(0, 7))?.Def == set.queen;
            promo.Unmake(undo);
            bool restored = promo.GetPiece(new Coord(0, 6)) == pawn && promo.GetPiece(new Coord(0, 7)) == null;
            Check("promotion detected, applied and undone", isPromo && becameQueen && restored);

            // ---------- AI ----------
            var profile = ScriptableObject.CreateInstance<AIProfile>();   // 초심자용 기본값
            var rng = new System.Random(1234);

            // 9. 1수 메이트는 반드시 둔다: 백 퀸 h5, 백 비숍 c4 대 흑 f7 폰(스콜라 메이트).
            var mate = set.OpeningPosition();
            Play(mate, 4, 1, 4, 3);   // e4
            Play(mate, 4, 6, 4, 4);   // e5
            Play(mate, 5, 0, 2, 3);   // Bc4
            Play(mate, 1, 7, 2, 5);   // Nc6
            Play(mate, 3, 0, 7, 4);   // Qh5
            Play(mate, 6, 7, 5, 5);   // Nf6??
            int found = 0;
            for (int i = 0; i < 20; i++)
            {
                var d = ChessAI.ChooseMove(mate, Team.Player, profile, rng, 10, set.queen);
                if (d.kind == AIDecisionKind.Mate && d.move.to == new Coord(5, 6)) found++;
            }
            Check("AI always plays mate in one (Qxf7#)", found == 20, found + "/20");

            // 10. 보호받는 폰에 퀸을 내던지지 않는다: 백 Qd1, e6이 보호하는 흑 폰 d5.
            var greed = new SearchBoard(8, 8);
            greed.Add(set.king, Team.Player, new Coord(4, 0));
            greed.Add(set.queen, Team.Player, new Coord(3, 0));
            greed.Add(set.king, Team.Enemy, new Coord(4, 7));
            greed.Add(set.pawn, Team.Enemy, new Coord(3, 4), hasMoved: true);
            greed.Add(set.pawn, Team.Enemy, new Coord(4, 5), hasMoved: true);
            int grabs = 0;
            for (int i = 0; i < 50; i++)
            {
                var d = ChessAI.ChooseMove(greed, Team.Player, profile, rng, 10, set.queen);
                if (d.move.to == new Coord(3, 4)) grabs++;
            }
            Check("AI does not hang its queen for a pawn", grabs == 0, grabs + "/50 grabs");

            // 11. AI끼리 대국: 모든 게임이 합법적인 수만으로 깔끔하게 끝나야 한다.
            int games = 6, totalPlies = 0, illegal = 0;
            var kinds = new Dictionary<AIDecisionKind, int>();
            var endings = new Dictionary<string, int>();
            var legal = new List<MoveOption>();
            for (int g = 0; g < games; g++)
            {
                var board = set.OpeningPosition();
                var side = Team.Player;
                string ending = "move limit";
                for (int ply = 0; ply < 200; ply++)
                {
                    var status = ChessRules.GetStatus(board, side);
                    if (status == ChessStatus.Checkmate || status == ChessStatus.Stalemate) { ending = status.ToString(); break; }

                    var d = ChessAI.ChooseMove(board, side, profile, rng, ply / 2, set.queen);
                    ChessRules.GetLegalMoves(board, d.move.piece, legal);
                    if (!MoveResolver.IsLegalTarget(legal, d.move.to, out _)) illegal++;
                    kinds[d.kind] = kinds.TryGetValue(d.kind, out var k) ? k + 1 : 1;

                    var promoteTo = ChessRules.IsPromotion(board, d.move.piece, d.move.to) ? set.queen : null;
                    board.Make(d.move.piece, d.move.to, promoteTo);
                    side = side.Opponent();
                    totalPlies++;
                }
                endings[ending] = endings.TryGetValue(ending, out var e) ? e + 1 : 1;
            }
            var mix = new StringBuilder();
            foreach (var kv in kinds) mix.Append(kv.Key).Append(' ').Append(kv.Value * 100 / Mathf.Max(1, totalPlies)).Append("% ");
            var ends = new StringBuilder();
            foreach (var kv in endings) ends.Append(kv.Key).Append('=').Append(kv.Value).Append(' ');
            Check("AI self-play stays legal", illegal == 0, games + " games, " + totalPlies + " moves, endings: " + ends + "| choices: " + mix);
            Object.DestroyImmediate(profile);

            log.AppendLine(failures == 0 ? "All checks passed." : failures + " check(s) failed.");
            if (failures == 0) Debug.Log(log.ToString());
            else Debug.LogError(log.ToString());

            set.Dispose();
            return failures == 0;
        }

        static void Play(SearchBoard board, int fx, int fy, int tx, int ty)
        {
            var piece = board.GetPiece(new Coord(fx, fy));
            board.Make(piece, new Coord(tx, ty));
        }

        static string Describe(SearchBoard board)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                var p = board.GetPiece(new Coord(x, y));
                sb.Append(p == null ? "." : p.Def.name[0] + (p.Team == Team.Player ? "w" : "b") + (p.HasMoved ? "m" : ""));
            }
            return sb.ToString() + "|" + board.AllPieces.Count;
        }

        /// <summary>
        /// 프로젝트의 기물 데이터를 불러온다. 수 개수가 표준 체스와 일치하도록
        /// 첫 두 칸 전진이 있는 체스 대결용 폰을 쓴다.
        /// </summary>
        sealed class PieceSet
        {
            public PieceDefinition king, queen, rook, bishop, knight, pawn;

            public static PieceSet Load()
            {
                PieceDefinition L(string n) => AssetDatabase.LoadAssetAtPath<PieceDefinition>(PieceFolder + n + ".asset");
                var set = new PieceSet
                {
                    king = L("King"), queen = L("Queen"), rook = L("Rook"),
                    bishop = L("Bishop"), knight = L("Knight")
                };
                // 체스 대결 전용 폰에는 첫 두 칸 전진이 들어 있다.
                set.pawn = L("Pawn_Duel");
                if (set.king == null || set.queen == null || set.rook == null || set.bishop == null
                    || set.knight == null || set.pawn == null)
                    return null;
                return set;
            }

            public SearchBoard OpeningPosition()
            {
                var b = new SearchBoard(8, 8);
                var back = new[] { rook, knight, bishop, queen, king, bishop, knight, rook };
                for (int x = 0; x < 8; x++)
                {
                    b.Add(back[x], Team.Player, new Coord(x, 0));
                    b.Add(pawn, Team.Player, new Coord(x, 1));
                    b.Add(pawn, Team.Enemy, new Coord(x, 6));
                    b.Add(back[x], Team.Enemy, new Coord(x, 7));
                }
                return b;
            }

            public void Dispose() => Object.DestroyImmediate(pawn);
        }
    }
}
