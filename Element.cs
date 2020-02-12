using System.Collections.Generic;

namespace GenDash
{
    public sealed class Direction {
        public int DirX { get; set; }
        public int DirY { get; set; }
    }
    public static class DirectionType {
        public static Direction Undefined = new Direction { DirX = 0, DirY = 0 };
        public static Direction Up = new Direction { DirX = 0, DirY = -1 };
        public static Direction Down = new Direction { DirX = 0, DirY = 1 };
        public static Direction Left = new Direction { DirX = -1, DirY = 0 };
        public static Direction Right = new Direction { DirX = 1, DirY = 0 };

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
        public bool Indestructible { get; set; } = false;

        public bool Rounded { get; set; } = false;
        public ExplosionType Explosion { get; set; } = ExplosionType.DoNotExplode;
        public Direction StartFacing { get; set; } = DirectionType.Undefined;
    }
    public class Element {
        public static ElementDetails Player = new ElementDetails { Symbols = { { DirectionType.Undefined, '@' } }, Explosion = ExplosionType.Explode };
        public static ElementDetails Space = new ElementDetails { Symbols = { { DirectionType.Undefined, '.' } } };
        public static ElementDetails Dirt = new ElementDetails { Symbols = { { DirectionType.Undefined, '*' } } };
        public static ElementDetails Boulder = new ElementDetails { Symbols = { { DirectionType.Undefined, '0' } }, Rounded = true };
        public static ElementDetails Diamond = new ElementDetails { Symbols = { { DirectionType.Undefined, 'd' } }, Rounded = true };
        public static ElementDetails Bricks = new ElementDetails { Symbols = { { DirectionType.Undefined, '#' } }, Rounded = true };
        public static ElementDetails Steel = new ElementDetails { Symbols = { { DirectionType.Undefined, '%' } }, Indestructible = true };

        public static ElementDetails Firefly = new ElementDetails {
            Symbols = {
                { DirectionType.Up, '^' },
                { DirectionType.Left, '<' },
                { DirectionType.Down, 'v' },
                { DirectionType.Right, '>' },
            },
            StartFacing = DirectionType.Left,
            Important = false,
            Explosion = ExplosionType.ExplodeToDiamond
        };

        public static ElementDetails Butterfly = new ElementDetails {
            Symbols = {
                { DirectionType.Up, 'M' },
                { DirectionType.Left, 'E' },
                { DirectionType.Down, 'W' },
                { DirectionType.Right, '3' },
            },
            StartFacing = DirectionType.Up,
            Important = false,
            Explosion = ExplosionType.Explode
        };

        public static ElementDetails Explosion0 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'a' } } };
        public static ElementDetails Explosion1 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'b' } } };
        public static ElementDetails Explosion2 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'c' } } };
        public static ElementDetails Explosion3 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'd' } } };
        public static ElementDetails Explosion4 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'e' } } };

        public static ElementDetails ExplosionToDiamond0 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'A' } } };
        public static ElementDetails ExplosionToDiamond1 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'B' } } };
        public static ElementDetails ExplosionToDiamond2 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'C' } } };
        public static ElementDetails ExplosionToDiamond3 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'D' } } };
        public static ElementDetails ExplosionToDiamond4 = new ElementDetails { Symbols = { { DirectionType.Undefined, 'E' } } };

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
        public static bool ExplodeSingle(Board board, int row, int col, ElementDetails nextElement) {
            Element element = board.GetElementAt(row, col);
            if (element == null || element.Details.Indestructible) return false;
            board.Place(new Element(nextElement) { Scanned = true }, row, col);

            return true;
        }
        public static bool Explode(Board board, int row, int col, bool toDiamond) {
            ElementDetails next = toDiamond ? ExplosionToDiamond1 : Explosion1;
            bool exploded = false;
            for (int r = -1; r <= 1; r++)
                for (int c = -1; c <= 1; c++) {
                    bool unit = ExplodeSingle(board, row + r, col + c, toDiamond ? ExplosionToDiamond1 : Explosion1);
                    unit = ExplodeSingle(board, row + r, col + c, next);
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
                board.Place(new Element(Explosion1), row, col);
                moved = true;
            } else
            if (Details == Explosion1) {
                board.Place(new Element(Explosion2), row, col);
                moved = true;
            } else
            if (Details == Explosion2) { 
                board.Place(new Element(Explosion3), row, col);
                moved = true;
            } else
            if (Details == Explosion3) {
                board.Place(new Element(Explosion4), row, col);
                moved = true;
            } else
            if (Details == Explosion4) {
                board.Place(new Element(Space), row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond0) {
                board.Place(new Element(ExplosionToDiamond1), row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond1) {
                board.Place(new Element(ExplosionToDiamond2), row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond2) {
                board.Place(new Element(ExplosionToDiamond3), row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond3) {
                board.Place(new Element(ExplosionToDiamond4), row, col);
                moved = true;
            } else
            if (Details == ExplosionToDiamond4) {
                board.Place(new Element(Diamond), row, col);
                moved = true;
            }
            return moved;
        }
        private static bool FoldPlayer(Board board, Element element, int row, int col) {
            Element dest = board.GetElementAt(row + board.InputY, col + board.InputX);

            if (dest == null || dest.Details == Space || dest.Details == Dirt || dest.Details == Diamond) {
                if (board.Grabbing) {
                    board.Place(new Element(Space), row + board.InputY, col + board.InputX);
                } else {
                    board.Place(new Element(Space), row, col);
                    board.Place(element, row + board.InputY, col + board.InputX);
                }
                element.Scanned = true;
                return true;
            } else {
                if (board.InputX != 0 && !dest.Scanned && dest.Details == Boulder) {
                    Element behind = board.GetElementAt(row, col + (board.InputX * 2));
                    if (behind == null || behind.Details == Space) {
                        if (board.Grabbing) {
                            board.Place(dest, row, col + (board.InputX * 2));
                            board.Place(new Element(Space), row, col + board.InputX);
                        } else {
                            board.Place(dest, row, col + (board.InputX * 2));
                            board.Place(new Element(Space), row, col);
                            board.Place(element, row, col + board.InputX);
                        }
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
            Element under = board.GetElementAt(row + 1, col);
            if (under == null || under.Details == Space) {
                element.Falling = true;
                board.Place(under, row, col);
                board.Place(element, row + 1, col);
                return true;
            } else {
                if (under.Details.Rounded) {
                    Element beside = leftFirst ? board.GetElementAt(row, col - 1) : board.GetElementAt(row, col + 1);
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
                } else
                if (under.Details.Explosion != ExplosionType.DoNotExplode) {
                    return Explode(board, row + 1, col, under.Details.Explosion == ExplosionType.ExplodeToDiamond);
                } else {
                    element.Falling = false;
                    return true;
                }
            }
            return false;
        }
        private static bool FoldMoving(Board board, int row, int col, Element element, bool leftFirst = true) {
            element.Scanned = true;
            Element under = board.GetElementAt(row + 1, col);
            if (under == null || under.Details == Space) {
                element.Falling = true;
                board.Place(under, row, col);
                board.Place(element, row + 1, col);
                return true;
            } else {
                if (under.Details.Rounded) {
                    Element beside = leftFirst ? board.GetElementAt(row, col - 1) : board.GetElementAt(row, col + 1);
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
            Element beside = board.GetElementAt(row - 1, col);
            bool explode = beside != null && beside.Details == Element.Player;
            if (!explode) {
                beside = board.GetElementAt(row, col - 1);
                explode = beside != null && beside.Details == Element.Player;
            }
            if (!explode) {
                beside = board.GetElementAt(row + 1, col);
                explode = beside != null && beside.Details == Element.Player;
            }
            if (!explode) {
                beside = board.GetElementAt(row, col + 1);
                explode = beside != null && beside.Details == Element.Player;
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