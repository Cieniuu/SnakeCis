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
public class game_clock
{
	public float delta_time_s { get; private set; }
	public int target_fps;
	private UInt64 cpu_tick_frequency;
	public game_clock()
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
public class game_window
{
	public System.IntPtr window_handle { get; private set; }
	public int width { get; private set; }
	public int height { get; private set; }

	public game_window(int w, int h)
	{
		width = w;
		height = h;
		SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
		if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
		{
			Console.WriteLine($"There was an issue initilizing SDL. {SDL.SDL_GetError()}");
		}

		// Create a new window given a title, size, and passes it a flag indicating it should be shown.
		window_handle = SDL.SDL_CreateWindow("Snake",
											SDL.SDL_WINDOWPOS_UNDEFINED,
											SDL.SDL_WINDOWPOS_UNDEFINED,
											width,
											height,
											SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

		if (window_handle == IntPtr.Zero)
		{
			Console.WriteLine($"There was an issue creating the window. {SDL.SDL_GetError()}");
		}
	}
}

public struct food
{
	public int pos_x;
	public int pos_y;
	public Game_Field_Type type;
}
public class game_state
{
	public game_window window { get; private set; }
	public System.IntPtr renderer { get; private set; }
	public game_clock clock { get; private set; }

	public Game_Run_State run_State;
	public int game_speed;
	public Game_Field_Type[,] play_field { get; private set; }

	public food apple;
	public food choco;

	public int score;
	public int play_field_size { get; private set; }

	private Random rand;

	public game_state(int max_play_field_size)
	{
		play_field = new Game_Field_Type[max_play_field_size, max_play_field_size];
		play_field_size = max_play_field_size;
		window = new game_window(900, 900);
		renderer = SDL.SDL_CreateRenderer(window.window_handle,
											   -1,
											   SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
											   SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

		if (renderer == IntPtr.Zero)
		{
			Console.WriteLine($"There was an issue creating the renderer. {SDL.SDL_GetError()}");
		}

		clock = new game_clock();
		rand = new Random();
		apple = new food { type = Game_Field_Type.Apple };
		choco = new food { type = Game_Field_Type.Choco };
    }

    private void place_on_random_position(ref food food)
    {
        if (play_field[food.pos_y, food.pos_x] == Game_Field_Type.Grass)
        {
            do
            {
                food.pos_x = rand.Next(0, 30);
                food.pos_y = rand.Next(0, 30);
            } while (play_field[food.pos_y, food.pos_x] != Game_Field_Type.Grass);

            play_field[food.pos_y, food.pos_x] = food.type;
        }
    }

    public void generate_random_food()
    {
        place_on_random_position(ref apple);
		place_on_random_position(ref choco);
	}

    public void reset_field_except_walls()
	{
		for (int y = 0; y < play_field_size; y++)
		{
			for (int x = 0; x < play_field_size; x++)
			{
				if (play_field[y, x] != Game_Field_Type.Wall)
					play_field[y, x] = Game_Field_Type.Grass;
			}
		}

		apple.pos_x = 0;
		apple.pos_y = 0;
		choco.pos_x = 0;
		choco.pos_y = 0;
	}
}
public struct body_element
{
	public float pos_x;
	public float pos_y;
}

public class snake
{
	public int snakeSize { get; private set; }
	public int vel_x;
	public int vel_y;
	public body_element[] body { get; private set; }
	private int snake_max_size;

	public snake(int snake_max_body_size)
	{
		snakeSize = 1;
		snake_max_size = snake_max_body_size;
		body = new body_element[snake_max_size];
	}

	public body_element check_snake_head_to_body_direction()
	{
		body_element output = new body_element();

		if (snakeSize > 1)
		{
			int head_x = (int)body[0].pos_x;
			int head_y = (int)body[0].pos_y;
			int body_x = (int)body[1].pos_x;
			int body_y = (int)body[1].pos_y;

			if (head_x == body_x)
			{
				if (head_y > body_y)
				{
					output.pos_x = 0.0f;
					output.pos_y = 1.0f;
				}
				else
				{
					output.pos_x = 0.0f;
					output.pos_y = -1.0f;
				}
			}
			if (head_y == body_y)
			{
				if (head_x > body_x)
				{
					output.pos_x = 1.0f;
					output.pos_y = 0.0f;
				}
				else
				{
					output.pos_x = -1.0f;
					output.pos_y = 0.0f;
				}
			}
		}
		else
		{
			output.pos_x = 0.0f;
			output.pos_y = 0.0f;
		}

		return output;
	}

	public void extend()
	{
		if (snakeSize + 1 >= snake_max_size)
		{
			return; //TODO: Log or error
		}

		float last_pos_x = body[snakeSize - 1].pos_x;
		float last_pos_y = body[snakeSize - 1].pos_y;

		if (snakeSize > 1)
		{
			float new_pos_x = last_pos_x - body[snakeSize - 2].pos_x;
			float new_pos_y = last_pos_y - body[snakeSize - 2].pos_y;

			if (new_pos_x == 0)
			{
				if (new_pos_y > 0)
				{
					body[snakeSize].pos_x = last_pos_x;
					body[snakeSize].pos_y = last_pos_y + 1;
					snakeSize++;
				}
				else
				{
					body[snakeSize].pos_x = last_pos_x;
					body[snakeSize].pos_y = last_pos_y - 1;
					snakeSize++;
				}
			}
			else
			{
				if (new_pos_x > 0)
				{
					body[snakeSize].pos_x = last_pos_x + 1;
					body[snakeSize].pos_y = last_pos_y;
					snakeSize++;
				}
				else
				{
					body[snakeSize].pos_x = last_pos_x - 1;
					body[snakeSize].pos_y = last_pos_y;
					snakeSize++;
				}
			}
		}
		else
		{
			body[snakeSize].pos_x = last_pos_x - vel_x;
			body[snakeSize].pos_y = last_pos_y - vel_y;
			snakeSize++;
		}
	}

	public void move(float dT_s, float speed)
	{
		float new_head_pos_x = body[0].pos_x + vel_x * dT_s * speed;
		float new_head_pos_y = body[0].pos_y + vel_y * dT_s * speed;

		if ((int)new_head_pos_x - (int)body[0].pos_x != 0 || (int)new_head_pos_y - (int)body[0].pos_y != 0)
		{
			for (int i = snakeSize - 1; i > 0; i--)
			{
				body[i].pos_x = body[i - 1].pos_x;
				body[i].pos_y = body[i - 1].pos_y;
			}
		}

		body[0].pos_x = new_head_pos_x;
		body[0].pos_y = new_head_pos_y;
	}

	public void reset_size()
	{
		snakeSize = 1;
	}
}


class Game
{
	static void gameplay_setup(game_state state, snake snake)
	{
		snake.reset_size();
		snake.body[0].pos_x = 15;
		snake.body[0].pos_y = 15;

		state.reset_field_except_walls();
		state.score = 0;
		state.game_speed = 14;
		state.run_State = Game_Run_State.Pause;
	}
	static void input_process(game_state state, snake snake)
	{
		body_element snake_head_dir = snake.check_snake_head_to_body_direction();

		SDL.SDL_Event event_sdl;
		SDL.SDL_PollEvent(out event_sdl);

		// Mouse input
		uint buttons = SDL.SDL_GetMouseState(out int mouse_x, out int mouse_y);
		if ((buttons & SDL.SDL_BUTTON_LMASK) != 0)
		{
			int tile_size = state.window.width / state.play_field_size;
			int tile_x = mouse_x / tile_size;
			int tile_y = mouse_y / tile_size;
			state.play_field[tile_y, tile_x] = Game_Field_Type.Wall;
		}
		if ((buttons & SDL.SDL_BUTTON_RMASK) != 0)
		{
			int tile_size = state.window.width / state.play_field_size;
			int tile_x = mouse_x / tile_size;
			int tile_y = mouse_y / tile_size;
			state.play_field[tile_y, tile_x] = Game_Field_Type.Grass;
		}

		// Keyboard input
		switch (event_sdl.type)
		{
			case SDL.SDL_EventType.SDL_QUIT:
				state.run_State = Game_Run_State.Quit;
				break;
			case SDL.SDL_EventType.SDL_KEYDOWN:
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
				{
					state.run_State = Game_Run_State.Quit;
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_UP)
				{
					if ((int)snake_head_dir.pos_y != 1)
					{
						snake.vel_x = 0;
						snake.vel_y = -1;
					}
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_DOWN)
				{
					if ((int)snake_head_dir.pos_y != -1)
					{
						snake.vel_x = 0;
						snake.vel_y = 1;
					}
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_RIGHT)
				{
					if ((int)snake_head_dir.pos_x != -1)
					{
						snake.vel_x = 1;
						snake.vel_y = 0;
					}
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_LEFT)
				{
					if ((int)snake_head_dir.pos_x != 1)
					{
						snake.vel_x = -1;
						snake.vel_y = 0;
					}
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_p)
				{
					if (state.run_State == Game_Run_State.Running)
						state.run_State = Game_Run_State.Pause;
					else if (state.run_State == Game_Run_State.Pause)
						state.run_State = Game_Run_State.Running;
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_c)
				{
					for (int y = 0; y < state.play_field_size; y++)
					{
						for (int x = 0; x < state.play_field_size; x++)
						{
							if (state.play_field[y, x] == Game_Field_Type.Wall)
							{
								state.play_field[y, x] = Game_Field_Type.Grass;
							}
						}
					}
				}
				break;
		}
	}

	static void update(game_state state, snake snake)
	{
		snake.move(state.clock.delta_time_s, state.game_speed);
		state.generate_random_food();

		// kolizja jablka i wiekszy pyton
		if ((int)snake.body[0].pos_x == state.apple.pos_x && (int)snake.body[0].pos_y == state.apple.pos_y)
		{
			snake.extend();
			state.play_field[state.apple.pos_y, state.apple.pos_x] = Game_Field_Type.Grass;
			state.score++;
		}

		//kolizja z koncem mapy
		if (snake.body[0].pos_x <= 0 || snake.body[0].pos_x >= state.play_field_size)
		{
			state.run_State = Game_Run_State.Restart;
		}
		else if (snake.body[0].pos_y <= 0 || snake.body[0].pos_y >= state.play_field_size)
		{
			state.run_State = Game_Run_State.Restart;
		}
		//kolizja ze scianami
		else if (state.play_field[(int)snake.body[0].pos_y, (int)snake.body[0].pos_x] == Game_Field_Type.Wall)
		{
			state.run_State = Game_Run_State.Restart;
		}

		//kolizja z cialem
		for (int i = 1; i < snake.snakeSize; i++)
		{
			if ((int)snake.body[0].pos_x == (int)snake.body[i].pos_x && (int)snake.body[0].pos_y == (int)snake.body[i].pos_y)
			{
				state.run_State = Game_Run_State.Restart;
			}
		}
	}

	static void render(game_state state, snake snake)
	{
		int tile_size = state.window.width / state.play_field_size;
		SDL.SDL_SetRenderDrawColor(state.renderer, 0, 0, 0, 255);
		SDL.SDL_RenderClear(state.renderer);

		SDL.SDL_Rect tile_rect = new SDL.SDL_Rect();

		for (int y = 0; y < state.play_field_size; y++)
		{
			for (int x = 0; x < state.play_field_size; x++)
			{
				tile_rect.x = x * tile_size;
				tile_rect.y = y * tile_size;
				tile_rect.w = tile_size - 1;
				tile_rect.h = tile_size - 1;

				switch (state.play_field[y, x])
				{
					case Game_Field_Type.Grass:
						SDL.SDL_SetRenderDrawColor(state.renderer, 1, 70, 52, 255);
						break;
					case Game_Field_Type.Apple:
						SDL.SDL_SetRenderDrawColor(state.renderer, 0, 255, 0, 255);
						break;
					case Game_Field_Type.Choco:
						SDL.SDL_SetRenderDrawColor(state.renderer, 150, 75, 0, 255);
						break;
					case Game_Field_Type.Wall:
						SDL.SDL_SetRenderDrawColor(state.renderer, 126, 126, 126, 255);
						break;
					default:
						break;
				}

				SDL.SDL_RenderFillRect(state.renderer, ref tile_rect);
			}
		}

		tile_rect.x = (int)snake.body[0].pos_x * tile_size;
		tile_rect.y = (int)snake.body[0].pos_y * tile_size;
		SDL.SDL_SetRenderDrawColor(state.renderer, 255, 0, 0, 255);
		SDL.SDL_RenderFillRect(state.renderer, ref tile_rect);

		for (int i = 1; i < snake.snakeSize; i++)
		{
			tile_rect.x = (int)snake.body[i].pos_x * tile_size;
			tile_rect.y = (int)snake.body[i].pos_y * tile_size;
			SDL.SDL_SetRenderDrawColor(state.renderer, 0, 0, 255, 255);
			SDL.SDL_RenderFillRect(state.renderer, ref tile_rect);
		}

		/* print_text(0, 3, 255, 255, 255, text, state, (char*)"SCORE %d", state.score);
			print_text(g_SCREEN_SIZE - 500, 3, 255, 255, 255, text, state, (char*)"FPS %.1f", 1.0f / state.clock.delta_time_s);*/

		SDL.SDL_RenderPresent(state.renderer);
	}

	static void terminate_SDL(game_state state)
	{
		SDL.SDL_DestroyRenderer(state.renderer);
		SDL.SDL_DestroyWindow(state.window.window_handle);
		SDL.SDL_Quit();
	}

	static void Main()
	{
		snake snake = new snake(512);
		game_state state = new game_state(30);
		state.clock.target_fps = 60;
		gameplay_setup(state, snake);
		//game_text text = { };
		/*   text.glyph_width = 18;
			text.glyph_height = 28;
			fonts_init(text, state);*/

		// Initilizes SDL_image for use with png files.
		if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) == 0)
		{
			Console.WriteLine($"There was an issue initilizing SDL2_Image {SDL_image.IMG_GetError()}");
		}

		// Main loop for the program
		while (state.run_State == Game_Run_State.Running || state.run_State == Game_Run_State.Pause)
		{
			System.UInt64 tick_start = SDL.SDL_GetPerformanceCounter();

			input_process(state, snake);

			if (state.run_State == Game_Run_State.Quit)
			{
				break;
			}
			if (state.run_State != Game_Run_State.Pause)
			{
				update(state, snake);
			}
			if (state.run_State == Game_Run_State.Restart)
			{
				gameplay_setup(state, snake);
			}

			render(state, snake);
			state.clock.clock_update_and_wait(tick_start);
		}

		terminate_SDL(state);
	}
}
