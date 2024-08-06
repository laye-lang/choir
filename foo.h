struct foo {
    int value;
};

typedef struct foo foo;

typedef struct bar
{
    int value;
} bar;

foo get_foo();
bar get_bar();

struct foo get_foo2();
struct bar get_bar2();
