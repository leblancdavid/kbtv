using Godot;

public interface IRoomBuilder
{
    void Build(WorldRoom world);
    void SetPlayer(CharacterBody2D player);
    Vector2 GridToWorld(Vector2I gridPos);
    CastShadowSystem GetShadows();
    Rect2 GetFloorBounds();
}
