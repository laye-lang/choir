typedef struct foo foo;
struct foo {
    int const value;
};

typedef struct bar
{
    int value;
    const struct foo foo1;
    const foo foo2;
} bar;

foo get_foo();
bar get_bar();

struct foo get_foo2();
struct bar get_bar2();
