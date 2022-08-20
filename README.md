```
dotnet run -c Release --ModelFile teapot.obj --UseLineFitter --MaxFrameRate 30 --Spin --PruneMap
```

![Teapot as Line Mess](docs/teapot_line.gif)

```
dotnet run -c Release --ModelFile teapot.obj --UseCharRay --MaxFrameRate 30 --Spin
```
[From](https://github.com/boxofyellow/Ascii3dEngine/commit/0ed839bb0871f6ed2ca72fc800f2c53c66349c12)
![Teapot as Ray Trace](docs/teapot_ray.gif)

[From](https://github.com/boxofyellow/Ascii3dEngine/commit/ac1f2c13e3a026f22818d82b8ed9f36a9c91b4d6)
![Teapot with Shadow](docs/teapot_shadow.gif)

[From](https://github.com/boxofyellow/Ascii3dEngine/commit/ffdbd909c46ac134803e61241dffc025d1a94e75)
![Teapot with Color](docs/teapot_color.gif)

[From](https://github.com/boxofyellow/Ascii3dEngine/commit/ec9deff351cc36d96c192c9f21c3b7f6438b6ee4)
![Teapot Real](docs/teapot_real.gif)

> **Note**: `--UseCharRay` [was removed](https://github.com/boxofyellow/Ascii3dEngine/commit/0ea672d9aaf0cc4a0bdb9b6eeb1b492359c237a4) (it is always on, --UseLineFitter was also removed at the same time)

```
dotnet run -c Release --ModelFile teapot.obj --MaxFrameRate 30 --Spin
```

[From](https://github.com/boxofyellow/Ascii3dEngine/commit/0ff303be1fa0c361558da009ea2f57b09a149b6f)
![Teapot Texture](docs/teapot_texture.gif)
