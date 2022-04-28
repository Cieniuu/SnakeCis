using SDL2;
using System;

public enum Game_Run_State
{
	Running,
	Quit,
	Pause,
	Restart
};
public enum Game_Field_Type
{
	Grass,
	Apple,
	Choco,
	Wall
};
public class Game_clock
{
	public float delta_time_s { get; private set; }
	public int target_fps;
	private UInt64 cpu_tick_frequency;
	public Game_clock()
	{
		cpu_tick_frequency = SDL.SDL_GetPerformanceFrequency();
	}

	public void clock_update_and_wait(System.UInt64 tick_start)
	{
		double time_work_s = get_seconds_elapsed_here(tick_start);
		double target_s = 1.0 / (double)target_fps;
		double time_to_wait_s = target_s - time_work_s;

		if (time_to_wait_s > 0 && time_to_wait_s < target_s)
		{
			SDL.SDL_Delay((System.UInt32)(time_to_wait_s * 1000));
			time_to_wait_s = get_seconds_elapsed_here(tick_start);
			while (time_to_wait_s < target_s)
				time_to_wait_s = get_seconds_elapsed_here(tick_start);
		}

		delta_time_s = (float)get_seconds_elapsed_here(tick_start);
	}
	public double get_seconds_elapsed_here(System.UInt64 end)
	{
		return (double)(SDL.SDL_GetPerformanceCounter() - end) / cpu_tick_frequency;
	}
}
public class Game_window
{
	public System.IntPtr Window_handle { get; private set; }
	public int Width { get; private set; }
	public int Height { get; private set; }

	public Game_window(int w, int h)
	{
		Width = w;
		Height = h;
		SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
		if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
		{
			Console.WriteLine($"There was an issue initilizing SDL. {SDL.SDL_GetError()}");
		}

		// Create a new window given a title, size, and passes it a flag indicating it should be shown.
		Window_handle = SDL.SDL_CreateWindow("Snake",
											SDL.SDL_WINDOWPOS_UNDEFINED,
											SDL.SDL_WINDOWPOS_UNDEFINED,
											Width,
											Height,
											SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

		if (Window_handle == IntPtr.Zero)
		{
			Console.WriteLine($"There was an issue creating the window. {SDL.SDL_GetError()}");
		}
	}
}

public struct Food
{
	public int x;
	public int y;
	public Game_Field_Type type;
}
public class Game_state
{

	public Game_clock Clock { get; private set; }

	public Game_Run_State run_State;
	public int game_speed;
	public Game_Field_Type[,] Play_field { get; private set; }

	public Food apple;
	public Food choco;

	public int score;
	public int Play_field_size { get; private set; }

	private Random rand;

	public Game_state(int max_play_field_size)
	{
		Play_field = new Game_Field_Type[max_play_field_size, max_play_field_size];
		Play_field_size = max_play_field_size;


		Clock = new Game_clock();
		rand = new Random();
		apple = new Food { type = Game_Field_Type.Apple };
		choco = new Food { type = Game_Field_Type.Choco };
	}

	private void Place_on_random_position(ref Food food)
	{
		if (Play_field[food.y, food.x] == Game_Field_Type.Grass)
		{
			do
			{
				food.x = rand.Next(0, Play_field_size);
				food.y = rand.Next(0, Play_field_size);
			} while (Play_field[food.y, food.x] != Game_Field_Type.Grass);

			Play_field[food.y, food.x] = food.type;
		}
	}

	public void Generate_random_food()
    {
        Place_on_random_position(ref apple);
		Place_on_random_position(ref choco);
	}

	public void Reset_field_except_walls()
	{
		for (int y = 0; y < Play_field_size; y++)
		{
			for (int x = 0; x < Play_field_size; x++)
			{
				if (Play_field[y, x] != Game_Field_Type.Wall)
					Play_field[y, x] = Game_Field_Type.Grass;
			}
		}

		apple.x = 0;
        apple.y = 0;
        choco.x = 0;
		choco.y = 0;

    }
}
public struct Body_element
{
	public float x;
	public float y;
}

public class Snake
{
	public int SnakeSize { get; private set; }
	public int vel_x;
	public int vel_y;
	public Body_element[] body { get; private set; }
	readonly int snake_max_size;

	public Snake(int snake_max_body_size)
	{
		SnakeSize = 1;
		vel_x = 1;
		vel_y = 0;
		snake_max_size = snake_max_body_size;
		body = new Body_element[snake_max_size];
	}

	public Body_element Check_relation_of_two_components(int first, int offset)
	{
		Body_element output = new Body_element();

		if (SnakeSize > 1)
		{
			if (first + offset < SnakeSize && first + offset > 0)
			{
				int first_x = (int)body[first].x;
				int first_y = (int)body[first].y;
				int next_x = (int)body[first + offset].x;
				int next_y = (int)body[first + offset].y;

				output.x = first_x - next_x;
				output.y = first_y - next_y;
			}
			else
			{
				output.x = 0;
				output.y = 0;
			}
		}
		else
		{
			output.x = vel_x;
			output.y = vel_y;
		}
		return output;
	}

	public void Extend()
	{
		if (SnakeSize + 1 >= snake_max_size)
		{
			return; //TODO: Log or error
		}

		float last_pos_x = body[SnakeSize - 1].x;
		float last_pos_y = body[SnakeSize - 1].y;

		if (SnakeSize > 1)
		{
			float new_pos_x = last_pos_x - body[SnakeSize - 2].x;
			float new_pos_y = last_pos_y - body[SnakeSize - 2].y;

			if (new_pos_x == 0)
			{
				if (new_pos_y > 0)
				{
					body[SnakeSize].x = last_pos_x;
					body[SnakeSize].y = last_pos_y + 1;
				}
				else
				{
					body[SnakeSize].x = last_pos_x;
					body[SnakeSize].y = last_pos_y - 1;
				}
			}
			else
			{
				if (new_pos_x > 0)
				{
					body[SnakeSize].x = last_pos_x + 1;
					body[SnakeSize].y = last_pos_y;
				}
				else
				{
					body[SnakeSize].x = last_pos_x - 1;
					body[SnakeSize].y = last_pos_y;
				}
			}
		}
		else
		{
			body[SnakeSize].x = last_pos_x - vel_x;
			body[SnakeSize].y = last_pos_y - vel_y;
		}

		SnakeSize++;
	}

	public void Shrink()
	{
		if (SnakeSize >= 1)
		{
			SnakeSize--;
		}
	}
	public void Move(float dT_s, float speed)
	{
		float new_head_pos_x = body[0].x + vel_x * dT_s * speed;
		float new_head_pos_y = body[0].y + vel_y * dT_s * speed;

		if ((int)new_head_pos_x - (int)body[0].x != 0 || (int)new_head_pos_y - (int)body[0].y != 0)
		{
			for (int i = SnakeSize - 1; i > 0; i--)
			{
				body[i].x = body[i - 1].x;
				body[i].y = body[i - 1].y;
			}
		}

		body[0].x = new_head_pos_x;
		body[0].y = new_head_pos_y;
	}

	public void Reset_size()
	{
		SnakeSize = 1;
	}
}

public class RenderContext
{
	public Game_window Window { get; private set; }
	public System.IntPtr Ctx { get; private set; }

	public RenderContext(int w, int h)
	{
		Window = new Game_window(w, h);
		Ctx = SDL.SDL_CreateRenderer(Window.Window_handle,
											   -1,
											   SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
											   SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

		if (Ctx == IntPtr.Zero)
		{
			Console.WriteLine($"There was an issue creating the renderer. {SDL.SDL_GetError()}");
		}

		if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) == 0)
		{
			Console.WriteLine($"There was an issue initilizing SDL2_Image {SDL_image.IMG_GetError()}");
		}
	}
	public IntPtr Texture_load(string filename)
	{
		IntPtr texture;
		texture = SDL_image.IMG_LoadTexture(Ctx, filename);

		return texture;
	}
}

public class Game
{
	public Game_state State { get; private set; }
	Snake snake;
	int tile_size;

	RenderContext renderer;
	readonly IntPtr texture_atlas;
	SDL.SDL_Rect tile_rect;
	SDL.SDL_Rect texture_rect;

	public Game()
	{
		renderer = new RenderContext(900, 900);
		snake = new Snake(200);
		State = new Game_state(15);
		State.Clock.target_fps = 60;
		texture_atlas = renderer.Texture_load("../../../textures/snake_atlas.png");
		Gameplay_setup();

		tile_size = renderer.Window.Width / State.Play_field_size;
		tile_rect = new SDL.SDL_Rect { w = tile_size, h = tile_size };
		texture_rect = new SDL.SDL_Rect { w = 64, h = 64 };
	}

	public void Gameplay_setup()
	{
		snake.Reset_size();
		snake.body[0].x = 7;
		snake.body[0].y = 7;

		State.Reset_field_except_walls();
		State.score = 0;
		State.game_speed = 11;
		State.run_State = Game_Run_State.Pause;
	}
	public void Input_process()
	{
		Body_element snake_head_dir = snake.Check_relation_of_two_components(0, 1);

		SDL.SDL_Event event_sdl;
		SDL.SDL_PollEvent(out event_sdl);

		// Mouse input
		uint buttons = SDL.SDL_GetMouseState(out int mouse_x, out int mouse_y);
		if ((buttons & SDL.SDL_BUTTON_LMASK) != 0)
		{
			int tile_size = renderer.Window.Width / State.Play_field_size;
			int tile_x = mouse_x / tile_size;
			int tile_y = mouse_y / tile_size;
			State.Play_field[tile_y, tile_x] = Game_Field_Type.Wall;
		}
		if ((buttons & SDL.SDL_BUTTON_RMASK) != 0)
		{
			int tile_size = renderer.Window.Width / State.Play_field_size;
			int tile_x = mouse_x / tile_size;
			int tile_y = mouse_y / tile_size;
			State.Play_field[tile_y, tile_x] = Game_Field_Type.Grass;
		}

		// Keyboard input
		switch (event_sdl.type)
		{
			case SDL.SDL_EventType.SDL_QUIT:
				State.run_State = Game_Run_State.Quit;
				break;
			case SDL.SDL_EventType.SDL_KEYDOWN:
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
				{
					State.run_State = Game_Run_State.Quit;
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_UP)
				{
					if ((int)snake_head_dir.y != 1)
					{
						snake.vel_x = 0;
						snake.vel_y = -1;
					}
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_DOWN)
				{
					if ((int)snake_head_dir.y != -1)
					{
						snake.vel_x = 0;
						snake.vel_y = 1;
					}
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_RIGHT)
				{
					if ((int)snake_head_dir.x != -1)
					{
						snake.vel_x = 1;
						snake.vel_y = 0;
					}
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_LEFT)
				{
					if ((int)snake_head_dir.x != 1)
					{
						snake.vel_x = -1;
						snake.vel_y = 0;
					}
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_p)
				{
					if (State.run_State == Game_Run_State.Running)
						State.run_State = Game_Run_State.Pause;
					else if (State.run_State == Game_Run_State.Pause)
						State.run_State = Game_Run_State.Running;
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_c)
				{
					for (int y = 0; y < State.Play_field_size; y++)
					{
						for (int x = 0; x < State.Play_field_size; x++)
						{
							if (State.Play_field[y, x] == Game_Field_Type.Wall)
							{
								State.Play_field[y, x] = Game_Field_Type.Grass;
							}
						}
					}
				}
				break;
		}
	}
	public void Update()
	{
		snake.Move(State.Clock.delta_time_s, State.game_speed);
		State.Generate_random_food();
		int headX = (int)snake.body[0].x;
		int headY = (int)snake.body[0].y;

		// kolizja jablka i wiekszy pyton
		if (headX == State.apple.x && headY == State.apple.y)
		{
			snake.Extend();
			State.Play_field[State.apple.y, State.apple.x] = Game_Field_Type.Grass;
			State.score++;
		}
		//kolizja z ciastkiem
		if (headX == State.choco.x && headY == State.choco.y)
		{
			snake.Shrink();
			State.Play_field[State.choco.y, State.choco.x] = Game_Field_Type.Grass;
			State.score--;
		}
		//cukrzyca
		if (snake.SnakeSize <= 0)
		{
			State.run_State = Game_Run_State.Restart;
		}

		//kolizja z koncem mapy
		if (headX < 0 || headX >= State.Play_field_size)
		{
			State.run_State = Game_Run_State.Restart;
		}
		else if (headY < 0 || headY >= State.Play_field_size)
		{
			State.run_State = Game_Run_State.Restart;
		}
		//kolizja ze scianami
		else if (State.Play_field[headY, headX] == Game_Field_Type.Wall)
		{
			State.run_State = Game_Run_State.Restart;
		}

		//kolizja z cialem
		for (int i = 1; i < snake.SnakeSize; i++)
		{
			if (headX == (int)snake.body[i].x && headY == (int)snake.body[i].y)
			{
				State.run_State = Game_Run_State.Restart;
			}
		}
	}

	public void Render()
	{
		SDL.SDL_SetRenderDrawColor(renderer.Ctx, 0, 0, 0, 255);
		SDL.SDL_RenderClear(renderer.Ctx);

		// Grass and wall back-filling
		for (int y = 0; y < State.Play_field_size; y++)
		{
			for (int x = 0; x < State.Play_field_size; x++)
			{
				tile_rect.x = x * tile_size;
				tile_rect.y = y * tile_size;

				texture_rect.x = 64;
				texture_rect.y = 128;
				SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);

				if (State.Play_field[y, x] == Game_Field_Type.Wall)
				{
					texture_rect.x = 0;
					texture_rect.y = 128;
					SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);
				}

			}
		}
		// apple render
		if (State.Play_field[State.apple.y, State.apple.x] == Game_Field_Type.Apple)
		{
			tile_rect.x = State.apple.x * tile_size;
			tile_rect.y = State.apple.y * tile_size;
			texture_rect.x = 0;
			texture_rect.y = 192;
			SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);
		}
		// choco render
		
		if (State.Play_field[State.choco.y, State.choco.x] == Game_Field_Type.Choco)
		{
			tile_rect.x = State.choco.x * tile_size;
			tile_rect.y = State.choco.y * tile_size;
			texture_rect.x = 64;
			texture_rect.y = 192;
			SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);
		}
		
		// Head rendering
		tile_rect.x = (int)snake.body[0].x * tile_size;
		tile_rect.y = (int)snake.body[0].y * tile_size;
		var head_pos = snake.Check_relation_of_two_components(0, 1);

		if ((int)head_pos.x == 1 && (int)head_pos.y == 0)
		{
			texture_rect.x = 256;
			texture_rect.y = 0;
		}
		else if ((int)head_pos.x == -1 && (int)head_pos.y == 0)
		{
			texture_rect.x = 192;
			texture_rect.y = 64;
		}
		else if ((int)head_pos.x == 0 && (int)head_pos.y == 1)
		{
			texture_rect.x = 256;
			texture_rect.y = 64;
		}
		else if ((int)head_pos.x == 0 && (int)head_pos.y == -1)
		{
			texture_rect.x = 192;
			texture_rect.y = 0;
		}
		SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);

		// Snake body rendering
		for (int i = 1; i < snake.SnakeSize; i++)
		{
			int currX = (int)snake.body[i].x;
			int currY = (int)snake.body[i].y;
			tile_rect.x = currX * tile_size;
			tile_rect.y = currY * tile_size;

			var prev_rel = snake.body[i - 1];

			if (i == snake.SnakeSize - 1)
			{
				if ((int)prev_rel.y < currY)
				{
					// Up
					texture_rect.x = 192;
					texture_rect.y = 128;
				}
				else if ((int)prev_rel.x > currX)
				{
					// Right
					texture_rect.x = 256;
					texture_rect.y = 128;
				}
				else if ((int)prev_rel.y > currY)
				{
					// Down
					texture_rect.x = 256;
					texture_rect.y = 192;
				}
				else if ((int)prev_rel.x < currX)
				{
					// Left
					texture_rect.x = 192;
					texture_rect.y = 192;
				}
			}
			else
			{
				var next_rel = snake.body[i + 1];

				if ((int)prev_rel.x < currX && (int)next_rel.x > currX || (int)next_rel.x < currX && (int)prev_rel.x > currX)
				{
					// 
					texture_rect.x = 64;
					texture_rect.y = 0;
				}
				else if ((int)prev_rel.x < currX && (int)next_rel.y > currY || (int)next_rel.x < currX && (int)prev_rel.y > currY)
				{
					// 
					texture_rect.x = 128;
					texture_rect.y = 0;
				}
				else if ((int)prev_rel.y < currY && (int)next_rel.y > currY || (int)next_rel.y < currY && (int)prev_rel.y > currY)
				{
					// 
					texture_rect.x = 128;
					texture_rect.y = 64;
				}
				else if ((int)prev_rel.y < currY && (int)next_rel.x < currX || (int)next_rel.y < currY && (int)prev_rel.x < currX)
				{
					//
					texture_rect.x = 128;
					texture_rect.y = 128;
				}
				else if ((int)prev_rel.x > currX && (int)next_rel.y < currY || (int)next_rel.x > currX && (int)prev_rel.y < currY)
				{
					// 
					texture_rect.x = 0;
					texture_rect.y = 64;
				}
				else if ((int)prev_rel.y > currY && (int)next_rel.x > currX || (int)next_rel.y > currY && (int)prev_rel.x > currX)
				{
					// 
					texture_rect.x = 0;
					texture_rect.y = 0;
				}
			}

			SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);
		}

		/* print_text(0, 3, 255, 255, 255, text, state, (char*)"SCORE %d", state.score);
			print_text(g_SCREEN_SIZE - 500, 3, 255, 255, 255, text, state, (char*)"FPS %.1f", 1.0f / state.clock.delta_time_s);*/

		SDL.SDL_RenderPresent(renderer.Ctx);
	}

	public void terminate_SDL()
	{
		SDL.SDL_DestroyRenderer(renderer.Ctx);
		SDL.SDL_DestroyWindow(renderer.Window.Window_handle);
		SDL.SDL_Quit();
	}
}

class Program
{
	static void Main()
	{
		Game game = new Game();

		// Main loop for the program
		while (game.State.run_State == Game_Run_State.Running || game.State.run_State == Game_Run_State.Pause)
		{
			System.UInt64 tick_start = SDL.SDL_GetPerformanceCounter();

			game.Input_process();

			if (game.State.run_State == Game_Run_State.Quit)
			{
				break;
			}
			if (game.State.run_State != Game_Run_State.Pause)
			{
				game.Update();
			}
			if (game.State.run_State == Game_Run_State.Restart)
			{
				game.Gameplay_setup();
			}

			game.Render();
			game.State.Clock.clock_update_and_wait(tick_start);
		}
		game.terminate_SDL();
	}
}