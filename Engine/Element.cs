using System;
using System.Collections.Generic;

namespace GenDash.Engine
{
    public sealed class Direction {
        public int DirX { get; set; }
        public int DirY { get; set; }
    }
    public static class DirectionType {
        public static readonly Direction Undefined = new() { DirX = 0, DirY = 0 };
        public static readonly Direction Up = new() { DirX = 0, DirY = -1 };
        public static readonly Direction Down = new() { DirX = 0, DirY = 1 };
        public static readonly Direction Left = new() { DirX = -1, DirY = 0 };
        public static readonly Direction Right = new() { DirX = 1, DirY = 0 };

        public static Direction ToLeft(Direction direction) {
            if (direction.DirX == Up.DirX && direction.DirY == Up.DirY) return Left;
            else
            if (direction.DirX == Left.DirX && direction.DirY == Left.DirY) return Down;
            else
            if (direction.DirX == Down.DirX && direction.DirY == Down.DirY) return Right;
            else
            if (direction.DirX == Right.DirX && direction.DirY == Right.DirY) return Up;
            return Undefined;
        }
        public static Direction ToRight(Direction direction) {
            if (direction.DirX == Up.DirX && direction.DirY == Up.DirY) return Right;
            else
            if (direction.DirX == Left.DirX && direction.DirY == Left.DirY) return Up;
            else
            if (direction.DirX == Down.DirX && direction.DirY == Down.DirY) return Left;
            else
            if (direction.DirX == Right.DirX && direction.DirY == Right.DirY) return Down;
            return Undefined;
        }
    }
    public enum ExplosionType {
        DoNotExplode, Explode, ExplodeToDiamond
    }
    public class ElementDetails {
        public IDictionary<Direction, char> Symbols { get; set; } = new Dictionary<Direction, char>();
        public bool Important { get; set; } = true;
        public bool Mob { get; set; } = false;
        public bool Indestructible { get; set; } = false;

        public bool Rounded { get; set; } = false;
        public ExplosionType Explosion { get; set; } = ExplosionType.DoNotExplode;
        public Direction StartFacing { get; set; } = DirectionType.Undefined;
    }
    public class Element {
        public static readonly ElementDetails Player = new() { Symbols = { { DirectionType.Undefined, '@' } }, Explosion = ExplosionType.Explode };
        public static readonly ElementDetails Space = new() { Symbols = { { DirectionType.Undefined, '.' } } };
        public static readonly ElementDetails Dirt = new() { Symbols = { { DirectionType.Undefined, '*' } } };
        public static readonly ElementDetails Boulder = new() { Symbols = { { DirectionType.Undefined, '0' } }, Rounded = true };
        public static readonly ElementDetails Diamond = new() { Symbols = { { DirectionType.Undefined, 'd' } }, Rounded = true };
        public static readonly ElementDetails Bricks = new() { Symbols = { { DirectionType.Undefined, '#' } }, Rounded = true };
        public static readonly ElementDetails Steel = new() { Symbols = { { DirectionType.Undefined, '%' } }, Indestructible = true };

        public static readonly ElementDetails Firefly = new()
        {
            Symbols = {
                { DirectionType.Up, '^' },
                { DirectionType.Left, '<' },
                { DirectionType.Down, 'v' },
                { DirectionType.Right, '>' },
            },
            StartFacing = DirectionType.Left,
            Important = false,
            Mob = true,
            Explosion = ExplosionType.ExplodeToDiamond
        };

        public static readonly ElementDetails Butterfly = new()
        {
            Symbols = {
                { DirectionType.Up, 'M' },
                { DirectionType.Left, 'E' },
                { DirectionType.Down, 'W' },
                { DirectionType.Right, '3' },
            },
            StartFacing = DirectionType.Up,
            Important = false,
            Mob = true,
            Explosion = ExplosionType.Explode
        };

        public static readonly ElementDetails Explosion0 = new() { Symbols = { { DirectionType.Undefined, '5' } } };
        public static readonly ElementDetails Explosion1 = new() { Symbols = { { DirectionType.Undefined, '6' } } };
        public static readonly ElementDetails Explosion2 = new() { Symbols = { { DirectionType.Undefined, '7' } } };
        public static readonly ElementDetails Explosion3 = new() { Symbols = { { DirectionType.Undefined, '8' } } };
        public static readonly ElementDetails Explosion4 = new() { Symbols = { { DirectionType.Undefined, '9' } } };

        public static readonly ElementDetails ExplosionToDiamond0 = new() { Symbols = { { DirectionType.Undefined, 'Y' } } };
        public static readonly ElementDetails ExplosionToDiamond1 = new() { Symbols = { { DirectionType.Undefined, 'U' } } };
        public static readonly ElementDetails ExplosionToDiamond2 = new() { Symbols = { { DirectionType.Undefined, 'I' } } };
        public static readonly ElementDetails ExplosionToDiamond3 = new() { Symbols = { { DirectionType.Undefined, 'P' } } };
        public static readonly ElementDetails ExplosionToDiamond4 = new() { Symbols = { { DirectionType.Undefined, 'T' } } };

        private static int IdCounter;
        public int Id { get; }
        public bool Scanned { get; set; }
        public bool Falling { get; set; }
        public ElementDetails Details { get; set; }

        public Direction Look { get; set; } = DirectionType.Undefined;

        public Element(ElementDetails details) {
            Id = IdCounter;
            Details = details;
            IdCounter++;
            Look = details.StartFacing;
        }
        public Element(ElementDetails details, Direction direction) {
            Id = IdCounter;
            Details = details;
            IdCounter++;
            Look = direction;
        }
        public static ElementDetails CharToElementDetails(char chr) {
            return chr switch
            {
                '@' => Player,
                '.' => Space,
                '*' => Dirt,
                '#' => Bricks,
                '0' => Boulder,
                'd' => Diamond,
                '%' => Steel,
                '^' or '<' or 'v' or '>' => Firefly,
                'M' or 'E' or 'W' or '3' => Butterfly,
                '5' => Explosion0,
                '6' => Explosion1,
                '7' => Explosion2,
                '8' => Explosion3,
                '9' => Explosion4,
                'Y' => ExplosionToDiamond0,
                'U' => ExplosionToDiamond1,
                'I' => ExplosionToDiamond2,
                'P' => ExplosionToDiamond3,
                'T' => ExplosionToDiamond4,
                _ => Steel,
            };
        }
        public static Direction CharToFacing(char chr) {
            return chr switch
            {
                '^' => DirectionType.Up,
                '<' => DirectionType.Left,
                'v' => DirectionType.Down,
                '>' => DirectionType.Right,
                'M' => DirectionType.Up,
                'E' => DirectionType.Left,
                'W' => DirectionType.Down,
                '3' => DirectionType.Right,
                _ => DirectionType.Undefined,
            };
        }

        public static bool ExplodeSingle(Board board, int row, int col, int fromRow, int fromCol, bool toDiamond) {
            var element = board.GetElementAt(row, col);
            if (element == null || element.Details.Indestructible) return false;
            var fromIndex = fromRow * board.ColCount + fromCol;
            var index = row * board.ColCount + col;
            var scanned = index <= fromIndex;
            ElementDetails next;
            if (toDiamond)
            {
                if (scanned) next = ExplosionToDiamond1; else next = ExplosionToDiamond0;
            }
            else
            {
                if (scanned) next = Explosion1; else next = Explosion0;
            }
            board.Place(new Element(next) { Scanned = index <= fromIndex }, row, col);

            return true;
        }
        public static bool Explode(Board board, int row, int col, bool toDiamond) {
            bool exploded = false;
            for (int r = -1; r <= 1; r++)
                for (int c = -1; c <= 1; c++) {
                    bool unit = ExplodeSingle(board, row + r, col + c, row, col, toDiamond);
                    exploded |= unit;
                }
            return exploded;
        }
        public bool Fold(Board board, int row, int col) {
            bool moved = false;
            if (Details == Player) {
                moved = FoldPlayer(board, this, row, col);
            } else
            if (Details == Boulder) {
                if (Falling)
                    moved = FoldFallingBoulder(board, this, row, col);
                else
                    moved = FoldBoulder(board, this, row, col);
            } else
            if (Details == Diamond) {
                if (Falling)
                    moved = FoldFallingDiamond(board, this, row, col);
                else
                    moved = FoldDiamond(board, this, row, col);
            } else
            if (Details == Butterfly) {
                moved = FoldButterfly(board, this, row, col);
            } else
            if (Details == Firefly) {
                moved = FoldFirefly(board, this, row, col);
            } else
            if (Details == Explosion0) {
                board.Place(new Element(Explosion1) { Scanned = true }, row, col);
                moved = true;
            } else
            if (Details == Explosion1) {
                board.Place(new Element(Explosion2) { Scanned = true }, row, col);
                moved = true;
            } else
            if (Details == Explosion2) { 
                board.Place(new Element(Explosion3) { Scanned = true }, row, col);
                moved = true;
            } else
            if (Details == Explosion3) {
                board.Place(new Element(Explosion4) { Scanned = true }, row, col);
                moved = true;
            } else
            if (Details == Explosion4) {
                board.Place(new Element(Space) { Scanned = true }, row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond0) {
                board.Place(new Element(ExplosionToDiamond1) { Scanned = true }, row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond1) {
                board.Place(new Element(ExplosionToDiamond2) { Scanned = true }, row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond2) {
                board.Place(new Element(ExplosionToDiamond3) { Scanned = true }, row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond3) {
                board.Place(new Element(ExplosionToDiamond4) { Scanned = true }, row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond4) {
                board.Place(new Element(Diamond) { Scanned = true }, row, col);
                moved = true;
            }
            return moved;
        }
        private static bool FoldPlayer(Board board, Element element, int row, int col) {
            var dest = board.GetElementAt(row + board.InputY, col + board.InputX);
            if (row + board.InputY == board.ExitY && col + board.InputX == board.ExitX)
                if (Array.Find(board.Data, x => x != null && x.Details == Diamond) == null) 
                    dest = new Element(Space);
            if (dest == null || dest.Details == Space || dest.Details == Dirt || dest.Details == Diamond) {
                element.Scanned = true;
                if (board.Grabbing) {
                    board.Place(new Element(Space), row + board.InputY, col + board.InputX);
                    //return (dest != null && dest.Details != Space); //NTS: "move was consumed and valid"
                } else {
                    board.Place(new Element(Space), row, col);
                    board.Place(element, row + board.InputY, col + board.InputX);
                    //return true;
                }
                return true; //NTS: "move was consumed", even if it didn't work in the case of grabbing a space/wall
            } else {
                if (board.InputX != 0 && dest.Details == Boulder && !dest.Falling) {
                    var behind = board.GetElementAt(row, col + board.InputX * 2);
                    if (behind == null || behind.Details == Space) {
                        if (board.Grabbing) {
                            board.Place(dest, row, col + board.InputX * 2);
                            board.Place(new Element(Space), row, col + board.InputX);
                        } else {
                            board.Place(dest, row, col + board.InputX * 2);
                            board.Place(new Element(Space), row, col);
                            board.Place(element, row, col + board.InputX);
                        }
                        dest.Scanned = true;
                        element.Scanned = true;
                        return true;
                    }
                }
            }
            return false;
        }
        private static bool FoldFallingBoulder(Board board, Element element, int row, int col) {
            return FoldFallingMoving(board, row, col, element, true);
        }
        private static bool FoldBoulder(Board board, Element element, int row, int col) {
            return FoldMoving(board, row, col, element, true);
        }
        private static bool FoldFallingDiamond(Board board, Element element, int row, int col) {
            return FoldFallingMoving(board, row, col, element, false);
        }
        private static bool FoldDiamond(Board board, Element element, int row, int col) {
            return FoldMoving(board, row, col, element, false);
        }
        private static bool FoldFallingMoving(Board board, int row, int col, Element element, bool leftFirst = true) {
            element.Scanned = true;
            var under = board.GetElementAt(row + 1, col);
            if (under == null || under.Details == Space) {
                element.Falling = true;
                board.Place(under, row, col);
                board.Place(element, row + 1, col);
                return true;
            } else {
                if (under.Details.Rounded && !under.Falling) {
                    var beside = leftFirst ? board.GetElementAt(row, col - 1) : board.GetElementAt(row, col + 1);
                    under = leftFirst ? board.GetElementAt(row + 1, col - 1) : board.GetElementAt(row + 1, col + 1);
                    if ((under == null || under.Details == Space) &&
                        (beside == null || beside.Details == Space)) {
                        board.Place(beside, row, col);
                        board.Place(element, row, leftFirst ? col - 1 : col + 1);
                        element.Falling = true;
                        return true;
                    } else {
                        beside = leftFirst ? board.GetElementAt(row, col + 1) : board.GetElementAt(row, col - 1);
                        under = leftFirst ? board.GetElementAt(row + 1, col + 1) : board.GetElementAt(row + 1, col - 1);
                        if ((under == null || under.Details == Space) &&
                            (beside == null || beside.Details == Space)) {
                            board.Place(beside, row, col);
                            board.Place(element, row, leftFirst ? col + 1 : col - 1);
                            element.Falling = true;
                            return true;
                        }
                        else 
                        {
                            element.Falling = false;
                            return true;
                        }
                    }
                } else
                if (under.Details.Explosion != ExplosionType.DoNotExplode) {
                    return Explode(board, row + 1, col, under.Details.Explosion == ExplosionType.ExplodeToDiamond);
                } else {
                    element.Falling = false;
                    return true;
                }
            }
        }
        private static bool FoldMoving(Board board, int row, int col, Element element, bool leftFirst = true) {
            element.Scanned = true;
            var under = board.GetElementAt(row + 1, col);
            if (under == null || under.Details == Space) {
                element.Falling = true;
                board.Place(under, row, col);
                board.Place(element, row + 1, col);
                return true;
            } else {
                if (under.Details.Rounded && !under.Falling) {
                    var beside = leftFirst ? board.GetElementAt(row, col - 1) : board.GetElementAt(row, col + 1);
                    under = leftFirst ? board.GetElementAt(row + 1, col - 1) : board.GetElementAt(row + 1, col + 1);
                    if ((under == null || under.Details == Space) &&
                        (beside == null || beside.Details == Space)) {
                        board.Place(beside, row, col);
                        board.Place(element, row, leftFirst ? col - 1 : col + 1);
                        element.Falling = true;
                        return true;
                    } else {
                        beside = leftFirst ? board.GetElementAt(row, col + 1) : board.GetElementAt(row, col - 1);
                        under = leftFirst ? board.GetElementAt(row + 1, col + 1) : board.GetElementAt(row + 1, col - 1);
                        if ((under == null || under.Details == Space) &&
                            (beside == null || beside.Details == Space)) {
                            board.Place(beside, row, col);
                            board.Place(element, row, leftFirst ? col + 1 : col - 1);
                            element.Falling = true;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool FoldFirefly(Board board, Element element, int row, int col) {

            return FoldWallFollower(board, element, row, col, true);
        }
        private static bool FoldButterfly(Board board, Element element, int row, int col) {

            return FoldWallFollower(board, element, row, col, false);
        }
        private static bool FoldWallFollower(Board board, Element element, int row, int col, bool leftFirst) {
            var beside = board.GetElementAt(row - 1, col);
            bool explode = beside != null && beside.Details == Player;
            if (!explode) {
                beside = board.GetElementAt(row, col - 1);
                explode = beside != null && beside.Details == Player;
            }
            if (!explode) {
                beside = board.GetElementAt(row + 1, col);
                explode = beside != null && beside.Details == Player;
            }
            if (!explode) {
                beside = board.GetElementAt(row, col + 1);
                explode = beside != null && beside.Details == Player;
            }
            if (explode) {
                return Explode(board, row, col, element.Details.Explosion == ExplosionType.ExplodeToDiamond);
            } else {

                Direction besideDir = leftFirst ? DirectionType.ToLeft(element.Look) : DirectionType.ToRight(element.Look);
                beside = board.GetElementAt(row + besideDir.DirY, col + besideDir.DirX);
                if (beside == null || beside.Details == Space) {
                    board.Place(beside, row, col);
                    board.Place(element, row + besideDir.DirY, col + besideDir.DirX);
                    element.Look = besideDir;
                    element.Scanned = true;
                    return true;
                } else {
                    beside = board.GetElementAt(row + element.Look.DirY, col + element.Look.DirX);
                    if (beside == null || beside.Details == Space) {
                        board.Place(beside, row, col);
                        board.Place(element, row + element.Look.DirY, col + element.Look.DirX);
                        element.Scanned = true;
                        return true;
                    } else {
                        element.Look = leftFirst ? DirectionType.ToRight(element.Look) : DirectionType.ToLeft(element.Look);
                        element.Scanned = true;
                        return true;
                    }
                }
            }
            //return false;
        }
    }
}