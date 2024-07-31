#include <choir/macros.hh>
#include <choir/core.hh>
#include <filesystem>
#include <llvm/ADT/IntrusiveRefCntPtr.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/Support/Error.h>
#include <llvm/Support/MemoryBuffer.h>
#include <llvm/Support/raw_ostream.h>
#include <llvm/Support/TargetSelect.h>
#include <llvm/Support/Unicode.h>
#include <mutex>
#include <random>
#include <thread>

#ifdef __linux__
#    include <unistd.h>
#endif

using namespace choir;

void StreamingDiagnosticsEngine::report_impl(Diagnostic&& diag) {
    Colors C(ctx.use_colours());
    using enum Color;

    // Give up if we’ve printed too many errors.
    if (error_limit and printed >= error_limit) {
        if (printed == error_limit) {
            printed++;

            stream << std::format(
                "\n{}{}Error:{}{} Too many errors emitted (> {}). Not showing any more errors.\n",
                C(Bold),
                Diagnostic::Colour(C, Diagnostic::Level::Error),
                C(Reset),
                C(Bold),
                printed - 1
            );

            stream << std::format(
                "{}Note:{}{} Use '--error-limit <limit>' to show more errors.{}\n",
                Diagnostic::Colour(C, Diagnostic::Level::Note),
                C(Reset),
                C(Bold),
                C(Reset)
            );
        }

        return;
    }

    // Reset the colour when we’re done.
    defer { stream << std::format("{}", C(Reset)); };

    // Make sure that diagnostics don’t clump together, but also don’t insert
    // an ugly empty line before the first diagnostic.
    if (printed != 0 and diag.level != Diagnostic::Level::Note) stream << "\n";
    printed++;

    // If the location is invalid, either because the specified file does not
    // exists, its position is out of bounds or 0, or its length is 0, then we
    // skip printing the location.
    auto l = diag.where.seek(ctx);
    if (not l.has_value()) {
        // Even if the location is invalid, print the file name if we can.
        if (auto f = ctx.file(diag.where.file_id))
            stream << std::format("{}{}: ", C(Bold), f->path());

        // Print the message.
        stream << std::format(
            "{}{}{}: {}{}{}{}\n",
            C(Bold),
            Diagnostic::Colour(C, diag.level),
            Diagnostic::Name(diag.level),
            C(Reset),
            C(Bold),
            diag.msg,
            C(Reset)
        );
        return;
    }

    // If the location is valid, get the line, line number, and column number.
    const auto [line, col, line_start, line_end] = *l;
    auto col_offs = col - 1;

    // Split the line into everything before the range, the range itself,
    // and everything after.
    std::string before(line_start, col_offs);
    std::string range(line_start + col_offs, std::min<uint64_t>(diag.where.len, uint64_t(line_end - (line_start + col_offs))));
    auto after = line_start + col_offs + diag.where.len > line_end
                   ? std::string{}
                   : std::string(line_start + col_offs + diag.where.len, line_end);

    // Replace tabs with spaces. We need to do this *after* splitting
    // because this invalidates the offsets.
    utils::ReplaceAll(before, "\t", "    ");
    utils::ReplaceAll(range, "\t", "    ");
    utils::ReplaceAll(after, "\t", "    ");

    // Print the file name, line number, and column number.
    const auto& file = *ctx.file(diag.where.file_id);
    stream << std::format("{}{}:{}:{}: ", C(Bold), file.name(), line, col);

    // Print the diagnostic name and message.
    stream << std::format("{}{}: ", Diagnostic::Colour(C, diag.level), Diagnostic::Name(diag.level));
    stream << std::format("{}{}\n", C(Reset), diag.msg);

    // Print the line up to the start of the location, the range in the right
    // colour, and the rest of the line.
    stream << std::format(" {} | {}", line, before);
    stream << std::format("{}{}{}{}", C(Bold), Diagnostic::Colour(C, diag.level), range, C(Reset));
    stream << std::format("{}\n", after);

    // Determine the number of digits in the line number.
    const auto digits = utils::NumberWidth(line);

    // LLVM’s columnWidthUTF8() function returns -1 for non-printable characters
    // for some ungodly reason, so guard against that.
    static const auto ColumnWidth = [](llvm::StringRef text) {
        auto wd = llvm::sys::unicode::columnWidthUTF8(text);
        return wd < 0 ? 0 : size_t(wd);
    };

    // Underline the range. For that, we first pad the line based on the number
    // of digits in the line number and append more spaces to line us up with
    // the range.
    for (size_t i = 0, end = digits + ColumnWidth(before) + sizeof("  | ") - 1; i < end; i++)
        stream << " ";

    // Finally, underline the range.
    stream << std::format("{}{}", C(Bold), Diagnostic::Colour(C, diag.level));
    for (size_t i = 0, end = ColumnWidth(range); i < end; i++) stream << "~";
    stream << "\n";
}
