using System.Numerics;

namespace WizardGame.Engine;

public sealed class Lemming
{
    private const float Epsilon = 0.001f;
    private readonly LemmingSettings _settings;
    private float _fallDistance;
    private int _builderStepsRemaining;
    private float _builderTimer;
    private float _diggerTimer;
    private float _basherTimer;
    private float _minerTimer;

    public Lemming(Vector2 position, bool facingRight = true)
        : this(position, facingRight, LemmingSettings.Default)
    {
    }

    public Lemming(Vector2 position, bool facingRight, LemmingSettings settings)
    {
        settings.Validate();
        Position = position;
        FacingRight = facingRight;
        _settings = settings;
    }

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public bool FacingRight { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public bool IsGrounded { get; private set; }
    public float Width => _settings.Width;
    public float Height => _settings.Height;
    public LemmingAbility Ability { get; private set; } = LemmingAbility.Walker;

    public void Update(World world, float deltaSeconds)
    {
        Update(world, deltaSeconds, 1f);
    }

    public void Update(World world, float deltaSeconds, float walkSpeedMultiplier)
    {
        if (!IsAlive)
        {
            return;
        }

        if (deltaSeconds <= 0f)
        {
            return;
        }

        if (Ability == LemmingAbility.Builder)
        {
            UpdateBuilder(world, deltaSeconds);
            return;
        }

        if (Ability == LemmingAbility.Digger)
        {
            UpdateDigger(world, deltaSeconds);
            return;
        }

        if (Ability == LemmingAbility.Basher)
        {
            UpdateBasher(world, deltaSeconds);
            return;
        }

        if (Ability == LemmingAbility.Miner)
        {
            UpdateMiner(world, deltaSeconds);
            return;
        }

        var wasGrounded = CheckGrounded(world);
        IsGrounded = wasGrounded;

        if (wasGrounded)
        {
            var direction = FacingRight ? 1f : -1f;
            var walkSpeed = _settings.WalkSpeed * MathF.Max(0.01f, walkSpeedMultiplier);
            var proposedX = Position.X + (direction * walkSpeed * deltaSeconds);

            if (!Collides(world, proposedX, Position.Y))
            {
                Position = new Vector2(proposedX, Position.Y);
            }
            else if (TryStepUp(world, proposedX, out var steppedY))
            {
                Position = new Vector2(proposedX, steppedY);
            }
            else
            {
                if (TryResolveHorizontalMove(world, Position.X, proposedX, out var resolvedX, out var blocked))
                {
                    if (MathF.Abs(resolvedX - Position.X) > Epsilon)
                    {
                        Position = new Vector2(resolvedX, Position.Y);
                    }
                    else if (blocked)
                    {
                        FacingRight = !FacingRight;
                    }
                }
                else
                {
                    FacingRight = !FacingRight;
                }
            }
        }

        var oldY = Position.Y;
        var fallSpeed = wasGrounded ? 0f : _settings.FallSpeed;
        Velocity = new Vector2(0f, fallSpeed);
        var proposedY = Position.Y + fallSpeed * deltaSeconds;
        if (Velocity.Y > 0f)
        {
            if (TryResolveFall(world, proposedY, out var landingY))
            {
                Position = new Vector2(Position.X, landingY);
                Velocity = new Vector2(Velocity.X, 0f);
            }
            else
            {
                Position = new Vector2(Position.X, proposedY);
            }
        }
        else if (!Collides(world, Position.X, proposedY))
        {
            Position = new Vector2(Position.X, proposedY);
        }
        else
        {
            Velocity = new Vector2(Velocity.X, 0f);
        }

        var fallDelta = MathF.Max(0f, Position.Y - oldY);
        if (fallDelta > 0f)
        {
            _fallDistance += fallDelta;
        }

        IsGrounded = CheckGrounded(world);
        if (IsGrounded)
        {
            Position = new Vector2(Position.X, SnapToGround(world));
            if (fallDelta > 0f && _fallDistance > _settings.MaxSafeFall * Height)
            {
                IsAlive = false;
                return;
            }

            _fallDistance = 0f;
        }
    }

    public bool TryStartBuilder()
    {
        if (!IsAlive || Ability == LemmingAbility.Builder)
        {
            return false;
        }

        Ability = LemmingAbility.Builder;
        _builderStepsRemaining = _settings.BuilderSteps;
        _builderTimer = 0f;
        _diggerTimer = 0f;
        _basherTimer = 0f;
        _minerTimer = 0f;
        _fallDistance = 0f;
        return true;
    }

    public bool TryStartDigger()
    {
        if (!IsAlive || Ability == LemmingAbility.Digger)
        {
            return false;
        }

        Ability = LemmingAbility.Digger;
        _builderStepsRemaining = 0;
        _builderTimer = 0f;
        _diggerTimer = 0f;
        _basherTimer = 0f;
        _minerTimer = 0f;
        _fallDistance = 0f;
        return true;
    }

    public bool TryStartBasher()
    {
        if (!IsAlive || Ability == LemmingAbility.Basher)
        {
            return false;
        }

        Ability = LemmingAbility.Basher;
        _builderStepsRemaining = 0;
        _builderTimer = 0f;
        _diggerTimer = 0f;
        _basherTimer = 0f;
        _minerTimer = 0f;
        _fallDistance = 0f;
        return true;
    }

    public bool TryStartMiner()
    {
        if (!IsAlive || Ability == LemmingAbility.Miner)
        {
            return false;
        }

        Ability = LemmingAbility.Miner;
        _builderStepsRemaining = 0;
        _builderTimer = 0f;
        _diggerTimer = 0f;
        _basherTimer = 0f;
        _minerTimer = 0f;
        _fallDistance = 0f;
        return true;
    }

    public bool HasBuilderSupport(World world)
    {
        if (!IsAlive)
        {
            return false;
        }

        var belowRow = (int)MathF.Floor(Position.Y + Height + Epsilon);
        var leftCell = (int)MathF.Floor(Position.X + Epsilon);
        var rightCell = (int)MathF.Floor(Position.X + Width - Epsilon);
        var totalCells = rightCell - leftCell + 1;
        if (totalCells <= 0)
        {
            return false;
        }

        var solidCells = 0;
        for (var cellX = leftCell; cellX <= rightCell; cellX++)
        {
            if (world.IsSolid(cellX, belowRow))
            {
                solidCells++;
            }
        }

        return solidCells * 2 > totalCells;
    }

    public bool HasDiggableGround(World world)
    {
        if (!IsAlive)
        {
            return false;
        }

        var belowRow = (int)MathF.Floor(Position.Y + Height + Epsilon);
        if (belowRow < 0 || belowRow >= world.Height)
        {
            return false;
        }

        var leftCell = (int)MathF.Floor(Position.X + Epsilon);
        var rightCell = (int)MathF.Floor(Position.X + Width - Epsilon);

        for (var cellX = leftCell; cellX <= rightCell; cellX++)
        {
            if (world.InBounds(cellX, belowRow) && world.IsSolid(cellX, belowRow))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasBashableTerrain(World world)
    {
        if (!IsAlive)
        {
            return false;
        }

        var frontX = FacingRight
            ? (int)MathF.Floor(Position.X + Width - Epsilon) + 1
            : (int)MathF.Floor(Position.X + Epsilon) - 1;

        if (frontX < 0 || frontX >= world.Width)
        {
            return false;
        }

        var topCell = (int)MathF.Floor(Position.Y + Epsilon);
        var bottomCell = (int)MathF.Floor(Position.Y + Height - Epsilon);

        for (var cellY = topCell; cellY <= bottomCell; cellY++)
        {
            if (world.InBounds(frontX, cellY) && world.IsSolid(frontX, cellY))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasMineableTerrain(World world)
    {
        if (!IsAlive)
        {
            return false;
        }

        var direction = FacingRight ? 1f : -1f;
        var newX = Position.X + direction;
        var newY = Position.Y + 1f;
        var leftCell = (int)MathF.Floor(newX + Epsilon);
        var rightCell = (int)MathF.Floor(newX + Width - Epsilon);
        var topCell = (int)MathF.Floor(newY + Epsilon);
        var bottomCell = (int)MathF.Floor(newY + Height - Epsilon);

        if (leftCell < 0 || rightCell >= world.Width || topCell < 0 || bottomCell >= world.Height)
        {
            return false;
        }

        for (var cellY = topCell; cellY <= bottomCell; cellY++)
        {
            for (var cellX = leftCell; cellX <= rightCell; cellX++)
            {
                if (world.IsSolid(cellX, cellY))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void StopBuilder()
    {
        Ability = LemmingAbility.Walker;
        _builderStepsRemaining = 0;
        _builderTimer = 0f;
    }

    private void StopDigger()
    {
        Ability = LemmingAbility.Walker;
        _diggerTimer = 0f;
    }

    private void StopBasher()
    {
        Ability = LemmingAbility.Walker;
        _basherTimer = 0f;
    }

    private void StopMiner()
    {
        Ability = LemmingAbility.Walker;
        _minerTimer = 0f;
    }

    private void UpdateBuilder(World world, float deltaSeconds)
    {
        if (!CheckGrounded(world))
        {
            StopBuilder();
            return;
        }

        _builderTimer += deltaSeconds;
        while (_builderTimer >= _settings.BuilderStepIntervalSeconds)
        {
            _builderTimer -= _settings.BuilderStepIntervalSeconds;
            if (_builderStepsRemaining <= 0)
            {
                StopBuilder();
                return;
            }

            if (!TryPlaceBuilderStep(world))
            {
                StopBuilder();
                return;
            }

            _builderStepsRemaining--;
            if (_builderStepsRemaining <= 0)
            {
                StopBuilder();
                return;
            }
        }
    }

    private void UpdateDigger(World world, float deltaSeconds)
    {
        if (!HasDiggableGround(world))
        {
            StopDigger();
            return;
        }

        _diggerTimer += deltaSeconds;
        while (_diggerTimer >= _settings.DiggerStepIntervalSeconds)
        {
            _diggerTimer -= _settings.DiggerStepIntervalSeconds;
            if (!TryDigStep(world))
            {
                StopDigger();
                return;
            }
        }
    }

    private void UpdateBasher(World world, float deltaSeconds)
    {
        if (!CheckGrounded(world) || !HasBashableTerrain(world))
        {
            StopBasher();
            return;
        }

        _basherTimer += deltaSeconds;
        while (_basherTimer >= _settings.BasherStepIntervalSeconds)
        {
            _basherTimer -= _settings.BasherStepIntervalSeconds;
            if (!TryBashStep(world))
            {
                StopBasher();
                return;
            }
        }
    }

    private void UpdateMiner(World world, float deltaSeconds)
    {
        if (!CheckGrounded(world) || !HasMineableTerrain(world))
        {
            StopMiner();
            return;
        }

        _minerTimer += deltaSeconds;
        while (_minerTimer >= _settings.MinerStepIntervalSeconds)
        {
            _minerTimer -= _settings.MinerStepIntervalSeconds;
            if (!TryMineStep(world))
            {
                StopMiner();
                return;
            }
        }
    }

    private bool TryPlaceBuilderStep(World world)
    {
        var direction = FacingRight ? 1f : -1f;
        var leftCell = (int)MathF.Floor(Position.X + Epsilon);
        var rightCell = (int)MathF.Floor(Position.X + Width - Epsilon);
        var footprintWidth = Math.Max(1, rightCell - leftCell + 1);
        var buildWidth = footprintWidth + 1;
        var frontX = FacingRight
            ? rightCell + 1
            : leftCell - 1;
        var footRow = (int)MathF.Floor(Position.Y + Height - Epsilon);

        for (var offset = 0; offset < buildWidth; offset++)
        {
            var tileX = frontX + (int)(direction * offset);
            if (!world.InBounds(tileX, footRow) || world.IsSolid(tileX, footRow))
            {
                return false;
            }
        }

        var newX = Position.X + direction;
        var newY = Position.Y - 1f;
        if (Collides(world, newX, newY))
        {
            return false;
        }

        for (var offset = 0; offset < buildWidth; offset++)
        {
            var tileX = frontX + (int)(direction * offset);
            world.SetTile(tileX, footRow, TileType.Solid);
        }
        Position = new Vector2(newX, newY);
        IsGrounded = CheckGrounded(world);
        return true;
    }

    private bool TryDigStep(World world)
    {
        var belowRow = (int)MathF.Floor(Position.Y + Height + Epsilon);
        if (belowRow < 0 || belowRow >= world.Height)
        {
            return false;
        }

        var leftCell = (int)MathF.Floor(Position.X + Epsilon);
        var rightCell = (int)MathF.Floor(Position.X + Width - Epsilon);
        var hadSolid = false;

        for (var cellX = leftCell; cellX <= rightCell; cellX++)
        {
            if (!world.InBounds(cellX, belowRow))
            {
                continue;
            }

            if (world.IsSolid(cellX, belowRow))
            {
                hadSolid = true;
                world.SetTile(cellX, belowRow, TileType.Empty);
            }
        }

        if (!hadSolid)
        {
            return false;
        }

        var newY = Position.Y + 1f;
        if (Collides(world, Position.X, newY))
        {
            return false;
        }

        Position = new Vector2(Position.X, newY);
        Velocity = Vector2.Zero;
        IsGrounded = CheckGrounded(world);
        _fallDistance = 0f;
        return true;
    }

    private bool TryBashStep(World world)
    {
        var direction = FacingRight ? 1f : -1f;
        var frontX = FacingRight
            ? (int)MathF.Floor(Position.X + Width - Epsilon) + 1
            : (int)MathF.Floor(Position.X + Epsilon) - 1;

        if (frontX < 0 || frontX >= world.Width)
        {
            return false;
        }

        var topCell = (int)MathF.Floor(Position.Y + Epsilon);
        var bottomCell = (int)MathF.Floor(Position.Y + Height - Epsilon);
        var hadSolid = false;

        for (var cellY = topCell; cellY <= bottomCell; cellY++)
        {
            if (!world.InBounds(frontX, cellY))
            {
                continue;
            }

            if (world.IsSolid(frontX, cellY))
            {
                hadSolid = true;
                world.SetTile(frontX, cellY, TileType.Empty);
            }
        }

        if (!hadSolid)
        {
            return false;
        }

        var newX = Position.X + direction;
        if (Collides(world, newX, Position.Y))
        {
            return false;
        }

        Position = new Vector2(newX, Position.Y);
        Velocity = Vector2.Zero;
        IsGrounded = CheckGrounded(world);
        _fallDistance = 0f;
        return true;
    }

    private bool TryMineStep(World world)
    {
        var direction = FacingRight ? 1f : -1f;
        var newX = Position.X + direction;
        var newY = Position.Y + 1f;
        var leftCell = (int)MathF.Floor(newX + Epsilon);
        var rightCell = (int)MathF.Floor(newX + Width - Epsilon);
        var topCell = (int)MathF.Floor(newY + Epsilon);
        var bottomCell = (int)MathF.Floor(newY + Height - Epsilon);
        var hadSolid = false;

        if (leftCell < 0 || rightCell >= world.Width || topCell < 0 || bottomCell >= world.Height)
        {
            return false;
        }

        for (var cellY = topCell; cellY <= bottomCell; cellY++)
        {
            for (var cellX = leftCell; cellX <= rightCell; cellX++)
            {
                if (world.IsSolid(cellX, cellY))
                {
                    hadSolid = true;
                    world.SetTile(cellX, cellY, TileType.Empty);
                }
            }
        }

        if (!hadSolid)
        {
            return false;
        }

        if (Collides(world, newX, newY))
        {
            return false;
        }

        Position = new Vector2(newX, newY);
        Velocity = Vector2.Zero;
        IsGrounded = CheckGrounded(world);
        _fallDistance = 0f;
        return true;
    }

    private bool CheckGrounded(World world)
    {
        return TryGetSupportRow(world, Position.X, Position.Y, out _);
    }

    private bool TryStepUp(World world, float proposedX, out float steppedY)
    {
        for (var step = 1; step <= _settings.MaxStepHeightTiles; step++)
        {
            var candidateY = Position.Y - step;
            if (candidateY < 0f)
            {
                break;
            }

            if (!Collides(world, proposedX, candidateY) && CheckGroundedAt(world, proposedX, candidateY))
            {
                steppedY = candidateY;
                return true;
            }
        }

        steppedY = 0f;
        return false;
    }

    private bool CheckGroundedAt(World world, float x, float y)
    {
        return TryGetSupportRow(world, x, y, out _);
    }

    private float SnapToGround(World world)
    {
        if (TryGetSupportRow(world, Position.X, Position.Y, out var supportRow))
        {
            return supportRow - Height;
        }

        return Position.Y;
    }

    private bool Collides(World world, float x, float y)
    {
        var right = x + Width - Epsilon;
        var bottom = y + Height - Epsilon;

        var leftCell = (int)MathF.Floor(x);
        var rightCell = (int)MathF.Floor(right);
        var topCell = (int)MathF.Floor(y);
        var bottomCell = (int)MathF.Floor(bottom);

        for (var cy = topCell; cy <= bottomCell; cy++)
        {
            for (var cx = leftCell; cx <= rightCell; cx++)
            {
                if (world.IsSolid(cx, cy))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsSolidAt(World world, float x, float y)
    {
        var cellX = (int)MathF.Floor(x);
        var cellY = (int)MathF.Floor(y);
        return world.IsSolid(cellX, cellY);
    }

    private bool TryResolveFall(World world, float proposedY, out float landingY)
    {
        var leftCell = (int)MathF.Floor(Position.X + Epsilon);
        var rightCell = (int)MathF.Floor(Position.X + Width - Epsilon);
        var startCell = (int)MathF.Floor(Position.Y + Height - Epsilon);
        var endCell = (int)MathF.Floor(proposedY + Height - Epsilon);

        for (var cellY = startCell + 1; cellY <= endCell; cellY++)
        {
            for (var cellX = leftCell; cellX <= rightCell; cellX++)
            {
                if (world.IsSolid(cellX, cellY))
                {
                    landingY = cellY - Height;
                    return true;
                }
            }
        }

        landingY = proposedY;
        return false;
    }

    private bool TryResolveHorizontalMove(World world, float currentX, float proposedX, out float resolvedX, out bool blocked)
    {
        blocked = false;
        if (MathF.Abs(proposedX - currentX) < Epsilon)
        {
            resolvedX = currentX;
            return true;
        }

        var topCell = (int)MathF.Floor(Position.Y);
        var bottomCell = (int)MathF.Floor(Position.Y + Height - Epsilon);

        if (proposedX > currentX)
        {
            var startRight = (int)MathF.Floor(currentX + Width - Epsilon);
            var endRight = (int)MathF.Floor(proposedX + Width - Epsilon);

            for (var cellX = startRight + 1; cellX <= endRight; cellX++)
            {
                for (var cellY = topCell; cellY <= bottomCell; cellY++)
                {
                    if (world.IsSolid(cellX, cellY))
                    {
                        resolvedX = cellX - Width;
                        blocked = true;
                        return true;
                    }
                }
            }
        }
        else
        {
            var startLeft = (int)MathF.Floor(currentX + Epsilon);
            var endLeft = (int)MathF.Floor(proposedX + Epsilon);

            for (var cellX = startLeft - 1; cellX >= endLeft; cellX--)
            {
                for (var cellY = topCell; cellY <= bottomCell; cellY++)
                {
                    if (world.IsSolid(cellX, cellY))
                    {
                        resolvedX = cellX + 1f;
                        blocked = true;
                        return true;
                    }
                }
            }
        }

        resolvedX = proposedX;
        return true;
    }

    private bool TryGetSupportRow(World world, float x, float y, out int supportRow)
    {
        var belowRow = (int)MathF.Floor(y + Height + Epsilon);
        var leftCell = (int)MathF.Floor(x + Epsilon);
        var rightCell = (int)MathF.Floor(x + Width - Epsilon);

        for (var cellX = leftCell; cellX <= rightCell; cellX++)
        {
            if (world.IsSolid(cellX, belowRow))
            {
                supportRow = belowRow;
                return true;
            }
        }

        supportRow = 0;
        return false;
    }
}
