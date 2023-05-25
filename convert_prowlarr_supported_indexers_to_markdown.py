"""
Convert-ProwlarrSupportedIndexersToMarkdownTable.py

The purpose of this script is to export a markdown table for the wiki of the available indexers.
"""

import json
import logging
import sys
import argparse
from datetime import datetime
import requests
import iso639
import pycountry

# Constants
API_VERSION = "v1"
WIKI_1NEWLINE = "\n"
WIKI_2NEWLINE = "\n\n"
WIKI_ENCODING = "utf8"
WIKI_INFOLINK = "https://wiki.servarr.com/prowlarr/supported-indexers#"
GH_URL = "https://api.github.com/repos/Prowlarr/Prowlarr/commits"
GH_COMMIT_URL = "https://github.com/Prowlarr/Prowlarr/commit/"
LANG_DICT = {
    'af': 'Afrikaans',
    'af-ZA': 'Afrikaans (South Africa)',
    'ar': 'Arabic',
    'ar-AE': 'Arabic (U.A.E.)',
    'ar-BH': 'Arabic (Bahrain)',
    'ar-DZ': 'Arabic (Algeria)',
    'ar-EG': 'Arabic (Egypt)',
    'ar-IQ': 'Arabic (Iraq)',
    'ar-JO': 'Arabic (Jordan)',
    'ar-KW': 'Arabic (Kuwait)',
    'ar-LB': 'Arabic (Lebanon)',
    'ar-LY': 'Arabic (Libya)',
    'ar-MA': 'Arabic (Morocco)',
    'ar-OM': 'Arabic (Oman)',
    'ar-QA': 'Arabic (Qatar)',
    'ar-SA': 'Arabic (Saudi Arabia)',
    'ar-SY': 'Arabic (Syria)',
    'ar-TN': 'Arabic (Tunisia)',
    'ar-YE': 'Arabic (Yemen)',
    'az': 'Azeri (Latin)',
    'az-AZ': 'Azeri (Azerbaijan)',
    'be': 'Belarusian',
    'be-BY': 'Belarusian (Belarus)',
    'bg': 'Bulgarian',
    'bg-BG': 'Bulgarian (Bulgaria)',
    'bs-BA': 'Bosnian (Bosnia and Herzegovina)',
    'ca': 'Catalan',
    'ca-ES': 'Catalan (Spain)',
    'cs': 'Czech',
    'cs-CZ': 'Czech (Czech Republic)',
    'cy': 'Welsh',
    'cy-GB': 'Welsh (United Kingdom)',
    'da': 'Danish',
    'da-DK': 'Danish (Denmark)',
    'de': 'German',
    'de-AT': 'German (Austria)',
    'de-CH': 'German (Switzerland)',
    'de-DE': 'German (Germany)',
    'de-LI': 'German (Liechtenstein)',
    'de-LU': 'German (Luxembourg)',
    'dv': 'Divehi',
    'dv-MV': 'Divehi (Maldives)',
    'el': 'Greek',
    'el-GR': 'Greek (Greece)',
    'en': 'English',
    'en-AU': 'English (Australia)',
    'en-BZ': 'English (Belize)',
    'en-CA': 'English (Canada)',
    'en-CB': 'English (Caribbean)',
    'en-GB': 'English (United Kingdom)',
    'en-IE': 'English (Ireland)',
    'en-JM': 'English (Jamaica)',
    'en-NZ': 'English (New Zealand)',
    'en-PH': 'English (Republic of the Philippines)',
    'en-TT': 'English (Trinidad and Tobago)',
    'en-US': 'English (United States)',
    'en-ZA': 'English (South Africa)',
    'en-ZW': 'English (Zimbabwe)',
    'eo': 'Esperanto',
    'es': 'Spanish',
    'es-AR': 'Spanish (Argentina)',
    'es-BO': 'Spanish (Bolivia)',
    'es-CL': 'Spanish (Chile)',
    'es-CO': 'Spanish (Colombia)',
    'es-CR': 'Spanish (Costa Rica)',
    'es-DO': 'Spanish (Dominican Republic)',
    'es-EC': 'Spanish (Ecuador)',
    'es-ES': 'Spanish (Spain)',
    'es-GT': 'Spanish (Guatemala)',
    'es-HN': 'Spanish (Honduras)',
    'es-MX': 'Spanish (Mexico)',
    'es-NI': 'Spanish (Nicaragua)',
    'es-PA': 'Spanish (Panama)',
    'es-PE': 'Spanish (Peru)',
    'es-PR': 'Spanish (Puerto Rico)',
    'es-PY': 'Spanish (Paraguay)',
    'es-SV': 'Spanish (El Salvador)',
    'es-UY': 'Spanish (Uruguay)',
    'es-VE': 'Spanish (Venezuela)',
    'et': 'Estonian',
    'et-EE': 'Estonian (Estonia)',
    'eu': 'Basque',
    'eu-ES': 'Basque (Spain)',
    'fa': 'Farsi',
    'fa-IR': 'Farsi (Iran)',
    'fi': 'Finnish',
    'fi-FI': 'Finnish (Finland)',
    'fo': 'Faroese',
    'fo-FO': 'Faroese (Faroe Islands)',
    'fr': 'French',
    'fr-BE': 'French (Belgium)',
    'fr-CA': 'French (Canada)',
    'fr-CH': 'French (Switzerland)',
    'fr-FR': 'French (France)',
    'fr-LU': 'French (Luxembourg)',
    'fr-MC': 'French (Principality of Monaco)',
    'gl': 'Galician',
    'gl-ES': 'Galician (Spain)',
    'gu': 'Gujarati',
    'gu-IN': 'Gujarati (India)',
    'he': 'Hebrew',
    'he-IL': 'Hebrew (Israel)',
    'hi': 'Hindi',
    'hi-IN': 'Hindi (India)',
    'hr': 'Croatian',
    'hr-BA': 'Croatian (Bosnia and Herzegovina)',
    'hr-HR': 'Croatian (Croatia)',
    'hu': 'Hungarian',
    'hu-HU': 'Hungarian (Hungary)',
    'hy': 'Armenian',
    'hy-AM': 'Armenian (Armenia)',
    'id': 'Indonesian',
    'id-ID': 'Indonesian (Indonesia)',
    'is': 'Icelandic',
    'is-IS': 'Icelandic (Iceland)',
    'it': 'Italian',
    'it-CH': 'Italian (Switzerland)',
    'it-IT': 'Italian (Italy)',
    'ja': 'Japanese',
    'ja-JP': 'Japanese (Japan)',
    'ka': 'Georgian',
    'ka-GE': 'Georgian (Georgia)',
    'kk': 'Kazakh',
    'kk-KZ': 'Kazakh (Kazakhstan)',
    'kn': 'Kannada',
    'kn-IN': 'Kannada (India)',
    'ko': 'Korean',
    'ko-KR': 'Korean (Korea)',
    'kok': 'Konkani',
    'kok-IN': 'Konkani (India)',
    'ky': 'Kyrgyz',
    'ky-KG': 'Kyrgyz (Kyrgyzstan)',
    'lt': 'Lithuanian',
    'lt-LT': 'Lithuanian (Lithuania)',
    'lv': 'Latvian',
    'lv-LV': 'Latvian (Latvia)',
    'mi': 'Maori',
    'mi-NZ': 'Maori (New Zealand)',
    'mk': 'FYRO Macedonian',
    'mk-MK': 'FYRO Macedonian (Former Yugoslav Republic of Macedonia)',
    'mn': 'Mongolian',
    'mn-MN': 'Mongolian (Mongolia)',
    'mr': 'Marathi',
    'mr-IN': 'Marathi (India)',
    'ms': 'Malay',
    'ms-BN': 'Malay (Brunei Darussalam)',
    'ms-MY': 'Malay (Malaysia)',
    'mt': 'Maltese',
    'mt-MT': 'Maltese (Malta)',
    'nb': 'Norwegian (Bokmål)',
    'nb-NO': 'Norwegian (Bokmål) (Norway)',
    'nl': 'Dutch',
    'nl-BE': 'Dutch (Belgium)',
    'nl-NL': 'Dutch (Netherlands)',
    'nn-NO': 'Norwegian (Nynorsk) (Norway)',
    'ns': 'Northern Sotho',
    'ns-ZA': 'Northern Sotho (South Africa)',
    'pa': 'Punjabi',
    'pa-IN': 'Punjabi (India)',
    'pl': 'Polish',
    'pl-PL': 'Polish (Poland)',
    'ps': 'Pashto',
    'ps-AR': 'Pashto (Afghanistan)',
    'pt': 'Portuguese',
    'pt-BR': 'Portuguese (Brazil)',
    'pt-PT': 'Portuguese (Portugal)',
    'qu': 'Quechua',
    'qu-BO': 'Quechua (Bolivia)',
    'qu-EC': 'Quechua (Ecuador)',
    'qu-PE': 'Quechua (Peru)',
    'ro': 'Romanian',
    'ro-RO': 'Romanian (Romania)',
    'ru': 'Russian',
    'ru-RU': 'Russian (Russia)',
    'sa': 'Sanskrit',
    'sa-IN': 'Sanskrit (India)',
    'se': 'Sami',
    'se-FI': 'Sami (Finland)',
    'se-NO': 'Sami (Norway)',
    'se-SE': 'Sami (Sweden)',
    'sk': 'Slovak',
    'sk-SK': 'Slovak (Slovakia)',
    'sl': 'Slovenian',
    'sl-SI': 'Slovenian (Slovenia)',
    'sq': 'Albanian',
    'sq-AL': 'Albanian (Albania)',
    'sr-BA': 'Serbian (Bosnia and Herzegovina)',
    'sr-SP': 'Serbian (Serbia and Montenegro)',
    'sv': 'Swedish',
    'sv-FI': 'Swedish (Finland)',
    'sv-SE': 'Swedish (Sweden)',
    'sw': 'Swahili',
    'sw-KE': 'Swahili (Kenya)',
    'syr': 'Syriac',
    'syr-SY': 'Syriac (Syria)',
    'ta': 'Tamil',
    'ta-IN': 'Tamil (India)',
    'te': 'Telugu',
    'te-IN': 'Telugu (India)',
    'th': 'Thai',
    'th-TH': 'Thai (Thailand)',
    'tl': 'Tagalog',
    'tl-PH': 'Tagalog (Philippines)',
    'tn': 'Tswana',
    'tn-ZA': 'Tswana (South Africa)',
    'tr': 'Turkish',
    'tr-TR': 'Turkish (Turkey)',
    'tt': 'Tatar',
    'tt-RU': 'Tatar (Russia)',
    'ts': 'Tsonga',
    'uk': 'Ukrainian',
    'uk-UA': 'Ukrainian (Ukraine)',
    'ur': 'Urdu',
    'ur-PK': 'Urdu (Islamic Republic of Pakistan)',
    'uz': 'Uzbek (Latin)',
    'uz-UZ': 'Uzbek (Uzbekistan)',
    'vi': 'Vietnamese',
    'vi-VN': 'Vietnamese (Viet Nam)',
    'xh': 'Xhosa',
    'xh-ZA': 'Xhosa (South Africa)',
    'zh': 'Chinese',
    'zh-CN': 'Chinese (China)',
    'zh-HK': 'Chinese (Hong Kong)',
    'zh-MO': 'Chinese (Macau)',
    'zh-SG': 'Chinese (Singapore)',
    'zh-TW': 'Chinese (Taiwan)',
    'zu': 'Zulu',
    'zu-ZA': 'Zulu (South Africa)'
}


def escape_markdown(text):
    """
    Escape brackets in the given text for Markdown formatting.
    """
    return text.replace("[", "\\[").replace("]", "\\]")


def get_logger(name=__name__):
    """ Gets the logger for the given name"""
    logging.basicConfig(format='%(asctime)s %(message)s',
                        datefmt='%Y-%m-%d %H:%M:%S')
    logger = logging.getLogger(name)
    logger.setLevel(logging.INFO)
    return logger


def get_language_name(language_code, indexer=None):
    """Gets the language name from the language code
    :param language_code: The language code
    :return: The language name
    """
    _logger = get_logger('lang_parser')
    country = None
    if indexer is not None:
        _logger.debug("Parsing Indexer %s", indexer)
    _logger.debug("Trying to get Name of Language: %s", language_code)
    try:
        return LANG_DICT[language_code]
    except KeyError:
        None
    try:
        language = iso639.Language.match(language_code)
        _logger.debug("Found Language: %s", language)
    except iso639.LanguageNotFoundError:
        _logger.debug("Language not found in iso639: %s", language_code)
        split_lang = language_code.split("-")
        try:
            _logger.debug("Trying to get Language: %s", split_lang[0])
            language = iso639.Language.match(split_lang[0])
            _logger.debug("Found Parsed Split Language Code: %s", language)
            try:
                _logger.debug("Trying to get Country: %s", split_lang[1])
                country = pycountry.countries.get(alpha_2=split_lang[1])
            except LookupError:
                None
            finally:
                if country is not None:
                    country = country.name
                    _logger.debug("Found Parsed Split Country: %s", country)
                else:
                    country = 'Unknown'
                    _logger.warning(
                        "Unknown split country code [%s]: %s for %s", language_code, split_lang[1], indexer)
        except iso639.LanguageNotFoundError:
            language = 'Unknown'
            _logger.warning(
                "Unknown split language code [%s]: %s for %s", language_code, split_lang[0], indexer)
    return f'{language}{" (" + country + ")" if country is not None else ""}'


def get_request(url, request_headers=None, request_timeout=5, max_retries=3):
    retries = 0
    while retries < max_retries:
        try:
            if request_headers is not None:
                response = requests.get(
                    url=url, timeout=request_timeout, headers=request_headers)
            else:
                response = requests.get(url=url, timeout=request_timeout)
            response.raise_for_status()
            return response
        except requests.exceptions.Timeout as ex:
            print("Request timed out:", ex)
            sys.exit(408)  # Request Timeout
        except requests.exceptions.ConnectionError as ex:
            print("Connection Error:", ex)
            retries += 1
        except requests.exceptions.HTTPError as ex:
            print("HTTP Error:", ex)
            sys.exit(500)  # Server Error
        except requests.exceptions.RequestException as ex:
            print("Request Exception:", ex)
            sys.exit(502)  # Bad Gateway
    print("Maximum retries exceeded.")
    sys.exit(504)  # Gateway Timeout


def get_version(api_url, headers):
    """
    Retrieves the Prowlarr application version.
    """
    version_url = f"{api_url}/system/status"
    response = get_request(version_url, headers)
    version_obj = json.loads(response.content)
    return version_obj["version"]


def get_indexers(api_url, headers):
    """
    Retrieves the list of indexers from Prowlarr.
    """
    indexer_url = f"{api_url}/indexer/schema"
    response = get_request(indexer_url, headers)
    indexer_obj = json.loads(response.content)
    return indexer_obj


def build_markdown_table(indexers, privacy, protocol):
    """
    Builds a markdown table for the given indexers, privacy, and protocol.
    """
    logger = get_logger('build_markdown_table')
    table = "|Indexer|Description|Language|"
    table += WIKI_1NEWLINE
    table += "|:--|:--|:--|"
    table += WIKI_1NEWLINE
    logger.info("Building Markdown Table for %s, %s", privacy, protocol)
    for indexer in indexers:
        if indexer["privacy"] in privacy and indexer["protocol"] == protocol:
            name = escape_markdown(
                indexer["name"]).strip().replace(".", r'\.')
            logger.debug("Name: %s", name)
            info_link = indexer["infoLink"].replace(WIKI_INFOLINK, "")
            logger.debug("Info Link: %s", info_link)
            description = escape_markdown(
                indexer["description"]).strip('.').strip().replace(".", r'\.')
            logger.debug("Description: %s", description)
            lang = get_language_name(indexer["language"], name)
            logger.debug("Language: %s", lang)
            if indexer["indexerUrls"][0] is not None and indexer["indexerUrls"][0] != "":
                url = indexer["indexerUrls"][0]
                row = f"[{name}]({url}){{#{info_link}}}"
            else:
                row = f"{name}{{#{info_link}}}"
            table += f"|{row}|{description}|{lang}|\n"
    return table


def main(commit, build, app_apikey, output_file, app_base_url):
    # API URLs
    api_url = f"{app_base_url}/api/{API_VERSION}"

    # Headers
    headers = {"X-Api-Key": app_apikey}

    # Determine Commit
    if not commit:
        response = get_request(GH_URL)
        github_req = json.loads(response.content)
        commit = github_req[0]["sha"]
    logging.info("Commit is {%s}", commit)

    # Determine Version (Build)
    if not build:
        version_obj = get_version(api_url, headers)
        build = version_obj.replace("\n", "").replace("\r", "")

    # Get Indexer Data
    indexer_obj = get_indexers(api_url, headers)

    # Build Table Fields
    logging.info("Building Indexer Tables")
    # Public Usenet
    logging.info("Building: Usenet - Public")
    tbl_usenet_public = build_markdown_table(indexer_obj, ["public"], "usenet")
    # Private Usenet
    logging.info("Building: Usenet - Private")
    tbl_usenet_private = build_markdown_table(
        indexer_obj, ["private", "semiprivate"], "usenet")
    # Public Torrents
    logging.info("Building: Torrents - Public")
    tbl_torrent_public = build_markdown_table(
        indexer_obj, ["public"], "torrent")
    # Private Torrents
    logging.info("Building: Torrents - Private")
    tbl_torrent_private = build_markdown_table(
        indexer_obj, ["private", "semiprivate"], "torrent")

    # Page Header Info
    wiki_page_start = (
        WIKI_1NEWLINE
        + f"- Supported Trackers and Indexers as of Build `{build}` / [Commit: {commit}]({GH_COMMIT_URL.rstrip('/')}/{commit})"
        + WIKI_1NEWLINE
    )

    # Build Page Pieces
    tbl_fmt_tor = (
        WIKI_2NEWLINE +
        "## Torrents" +
        WIKI_2NEWLINE +
        "### Public Trackers" +
        WIKI_2NEWLINE +
        tbl_torrent_public +
        WIKI_1NEWLINE +
        "### Private & Semi-Private Trackers" +
        WIKI_2NEWLINE +
        tbl_torrent_private
    )

    tbl_fmt_use = (
        WIKI_2NEWLINE +
        "## Usenet" +
        WIKI_2NEWLINE +
        "### Public Indexers" +
        WIKI_2NEWLINE +
        tbl_usenet_public +
        WIKI_1NEWLINE +
        "### Private & Semi-Private Indexers" +
        WIKI_2NEWLINE +
        tbl_usenet_private
    )

    # Build and Output Page
    date = datetime.utcnow().isoformat()
    header_wiki = (
        f"---\n"
        f"title: Prowlarr Supported Indexers\n"
        f"description: Indexers currently named as supported in the current nightly build of Prowlarr. "
        f"Other indexers may be available via either Generic Newznab or Generic Torznab.\n"
        f"published: true\n"
        f"date: {date}\n"
        f"tags: prowlarr, indexers\n"
        f"editor: markdown\n"
        f"dateCreated: {date}\n"
        f"---"
    )
    wiki_page_version = (
        "---\n\n"
        "- Current `Master` Version | ![Current Master/Stable]"
        "(https://img.shields.io/badge/dynamic/json?color=f5f5f5&style=flat-square&label=Master"
        "&query=%24%5B0%5D.version&url=https://prowlarr.servarr.com/v1/update/master/changes)\n"
        "- Current `Develop` Version | ![Current Develop/Beta]"
        "(https://img.shields.io/badge/dynamic/json?color=f5f5f5&style=flat-square&label=Develop"
        "&query=%24%5B0%5D.version&url=https://prowlarr.servarr.com/v1/update/develop/changes)\n"
        "- Current `Nightly` Version | ![Current Nightly/Unstable]"
        "(https://img.shields.io/badge/dynamic/json?color=f5f5f5&style=flat-square&label=Nightly"
        "&query=%24%5B0%5D.version&url=https://prowlarr.servarr.com/v1/update/nightly/changes)\n"
        "---"
    )

    # Build and Output Page
    wiki_page_file = (
        f"{header_wiki}"
        f"{WIKI_1NEWLINE}"
        f"{wiki_page_start}"
        f"{WIKI_1NEWLINE}"
        f"{wiki_page_version}"
        f"{tbl_fmt_tor}"
        f"{tbl_fmt_use}"
    )
    with open(output_file, "w", encoding=WIKI_ENCODING) as file:
        file.write(wiki_page_file)
    logging.info("Wiki Page Output")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Convert Prowlarr Supported Indexers to Markdown Table"
    )
    parser.add_argument("-c", "--commit", help="Commit hash")
    parser.add_argument("-b", "--build", help="Build version")
    parser.add_argument("-k", "--appapikey", help="App API Key")
    parser.add_argument(
        "-o", "--outputfile", help="Output file path", default="supported-indexers.md"
    )
    parser.add_argument(
        "-u",
        "--appbaseurl",
        help="App Base URL",
        default="http://localhost:9696",
    )
    args = parser.parse_args()

    main(
        args.commit,
        args.build,
        args.appapikey,
        args.outputfile,
        args.appbaseurl,
    )
