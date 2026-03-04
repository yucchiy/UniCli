#!/usr/bin/env bash
set -euo pipefail

# Generate doc/commands.md from unicli commands --json output.

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
UNICLI_BIN="${ROOT_DIR}/.build/unicli"
OUTPUT="${ROOT_DIR}/doc/commands.md"

if ! command -v jq &>/dev/null; then
  echo "Error: jq is required. Install with 'brew install jq'." >&2
  exit 1
fi

if [[ ! -f "$UNICLI_BIN" ]]; then
  echo "Error: .build/unicli not found. Run 'dotnet publish src/UniCli.Client -o .build' first." >&2
  exit 1
fi

export UNICLI_PROJECT="${UNICLI_PROJECT:-${ROOT_DIR}/src/UniCli.Unity}"

JSON=$("$UNICLI_BIN" commands --json 2>/dev/null)

mkdir -p "$(dirname "$OUTPUT")"

# Use jq to transform JSON into markdown
echo "$JSON" | jq -r '
  .data | sort_by(.name) |

  # Group by module (or derive from command name)
  group_by(
    if (.module // "") != "" then .module
    elif (.name | contains(".")) then (.name | split(".")[0])
    else "Core"
    end
  ) |

  # Sort groups: Core first, then alphabetical
  sort_by(
    if .[0] |
      (if (.module // "") != "" then .module
       elif (.name | contains(".")) then (.name | split(".")[0])
       else "Core"
       end) == "Core"
    then ""
    else
      .[0] |
      if (.module // "") != "" then .module
      elif (.name | contains(".")) then (.name | split(".")[0])
      else "Core"
      end
    end
  ) |

  # Header
  "# Command Reference\n\n> Auto-generated from `unicli commands --json`. Run `tools/generate-command-reference.sh` to update.\n",

  # Each group
  (.[] |
    # Derive group name
    (.[0] |
      if (.module // "") != "" then .module
      elif (.name | contains(".")) then (.name | split(".")[0])
      else "Core"
      end
    ) as $group |

    "\n## \($group)\n",

    (.[] |
      "\n### \(.name)\n",

      (if (.description // "") != "" then "\(.description)\n" else "" end),

      # Parameters
      (if (.requestFields // [] | length) == 0 then
        "**Parameters:** None\n"
      else
        "**Parameters:**\n\n| Field | Type |\n|---|---|\n" +
        ([.requestFields[] |
          "`\(.name)` | `\(.type)`" +
          (if (.defaultValue // "") != "" then " (default: `\(.defaultValue)`)" else "" end)
        ] | map("| \(.) |") | join("\n")) + "\n"
      end),

      # Response
      (if (.responseFields // [] | length) == 0 then
        "**Response:** None\n"
      else
        "**Response:**\n\n| Field | Type |\n|---|---|\n" +
        ([.responseFields[] |
          "`\(.name)` | `\(.type)`" +
          (if (.defaultValue // "") != "" then " (default: `\(.defaultValue)`)" else "" end)
        ] | map("| \(.) |") | join("\n")) + "\n"
      end),

      "---\n"
    )
  )
' > "$OUTPUT"

COUNT=$(echo "$JSON" | jq '.data | length')
echo "Generated ${OUTPUT} (${COUNT} commands)"
