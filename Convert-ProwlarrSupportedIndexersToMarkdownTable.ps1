#Requires -Module FormatMarkdownTable -Version 7.1

<#
    .SYNOPSIS
        Name: Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1
        The purpose of this script is to export a markdown table for the wiki of the available indexers
    .DESCRIPTION
        Grabs build number and available indexers from a local or remotely installed Prowlarr instance. Requires App API Key. Gets latest commit from Github if Commit is not passed
    .NOTES
        This script has been tested on Windows PowerShell 7.1.3
    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -AppAPIKey "asjdhfjashdf89787asdfsad87676" -AppBaseURL http://prowlarr:9696 -OutputFile "supported-indexers.md"
    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -AppAPIKey "asjdhfjashdf89787asdfsad87676" -AppBaseURL http://prowlarr:9696
    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -Build "test" -AppAPIKey "asjdhfjashdf89787asdfsad87676" -OutputFile "supported-indexers.md"
    .EXAMPLE
    PS> .\Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1 -Commit 1.1.1.1 -AppAPIKey "asjdhfjashdf89787asdfsad87676"
#>

[CmdletBinding()]
param (
    [Parameter(Position = 1)]
    [string]$Commit,
    [Parameter(Position = 2)]
    [string]$Build,
    [Parameter(Mandatory, Position = 3)]
    [string]$AppAPIKey,
    [Parameter(Position = 4)]
    [System.IO.FileInfo]$OutputFile = ".$([System.IO.Path]::DirectorySeparatorChar)supported-indexers.md",
    [Parameter(Position = 5)]
    [uri]$AppBaseURL = 'http://localhost:9696'
),

# Gather Inputs & Variables
## User Inputs
### Convert Params to match vars
$app_baseUrl = $AppBaseURL
$app_apikey = $AppAPIKey
## Start Variables
### Application Details
$app_api_version = 'v1'
$app_api_path = '/api/'
$app_api_endpoint_version = '/system/status'
$app_api_endpoint_indexer = '/indexer/schema'
$headers = @{'X-Api-Key' = $app_apikey }
### Github App Info
$gh_app_org = 'Prowlarr'
$gh_app_repo = 'Prowlarr'
### Wiki Details
$wiki_link = 'https://wiki.servarr.com'
$wiki_app_path = '/prowlarr'
$wiki_page = 'supported-indexers'
$wiki_bookmark = '#'
### Page Formatting
$markdown_escape_regex = '(\w)(\.|\[|\])(\w)'
$markdown_escape_regex_rep = '$1\$2$3'
$wiki_1newline = "`r`n"
$wiki_2newline = "`r`n`r`n"
$wiki_encoding = 'utf8'
### Github Details
$gh_web = 'https://github.com'
$gh_web_commit = 'commit/'
## End Variables

Write-Information 'Variables and Inputs Imported'
## Build Parameters
### App
$api_url = ($app_baseUrl.ToString().TrimEnd('/')) + $app_api_path + $app_api_version
$version_url = $api_url + $app_api_endpoint_version
$indexer_url = $api_url + $app_api_endpoint_indexer
### Github
$gh_repo_org = $gh_app_org + '/' + $gh_app_repo + '/'
### Wiki
$wiki_infolink = ($wiki_link.ToString().TrimEnd('/')) + $wiki_app_path + '/' + $wiki_page + $wiki_bookmark
$wiki_commiturl = ($gh_web.ToString().TrimEnd('/')) + '/' + $gh_repo_org + $gh_web_commit
Write-Information 'Parameters Built'

## Invoke Requests & Convert to Objects
Write-Information 'Getting Version Data and Converting Response to Object'
$version_obj = (Invoke-WebRequest -Uri $version_url -Headers $headers -ContentType 'application/json' -Method Get).Content | ConvertFrom-Json
Write-Information 'Got App Version'
Write-Information 'Getting Indexer Data and Converting Response to Object'
$indexer_obj = (Invoke-WebRequest -Uri $indexer_url -Headers $headers -ContentType 'application/json' -Method Get).Content | ConvertFrom-Json
Write-Information 'Got Indexer Data'
## We use the regex to escape dots (URLS) and Brackets (markdown links) in names and descriptions
$indexer_name_exp = { IF ($_.IndexerUrls) { '[' + ($_.name -replace $markdown_escape_regex, $markdown_escape_regex_rep) + '](' + ($_.IndexerUrls[0]) + ')' + '{#' + $_.infoLink.Replace($wiki_infolink.ToString(), '') + '}' } Else { ($_.name -replace $markdown_escape_regex, $markdown_escape_regex_rep) + '{#' + $_.infoLink.Replace($wiki_infolink.ToString(), '') + '}' } }
$usenet_indexer_name_exp = { IF ($_.IndexerUrls) { '[' + ($_.name -replace $markdown_escape_regex, $markdown_escape_regex_rep) + '](' + ($_.IndexerUrls[0]) + ')' + '{#' + $_.infoLink.Replace($wiki_infolink.ToString(), '') + '}' } Else { IF ($_.fields.value[0]) { '[' + ($_.name -replace $markdown_escape_regex, $markdown_escape_regex_rep) + '](' + ($_.fields.value[0].Replace('api.', '').Replace('feed.', '')) + ')' + '{#' + $_.infoLink.Replace($wiki_infolink.ToString(), '') + '}' } Else { ($_.name -replace $markdown_escape_regex, $markdown_escape_regex_rep) + '{#' + $_.infoLink.Replace($wiki_infolink.ToString(), '') + '}' } } }
$indexer_description_exp = { ($_.description -replace $markdown_escape_regex, $markdown_escape_regex_rep) }


$gh_app_org = 'Prowlarr'
$gh_app_repo = 'Prowlarr'
$gh_repo_org = $gh_app_org + '/' + $gh_app_repo + '/'
## Determine Commit
if ( $PSBoundParameters.ContainsKey('commit') )
{
        Write-Information "Commit passed from argument. Skipping Github Query"
}
else

{
    $gh_url = ('https://api.github.com/repos/' + $gh_repo_org + 'commits')
    Write-Information "Getting commit info from Github [$gh_url]"
    $github_req = Invoke-RestMethod -Uri $gh_url -ContentType 'application/json' -Method Get
    $commit = ($github_req | select-object -first 1).sha
}
Write-Information "Commit is $commit"
## Determine Commit

## Determine Version (Build)
Write-Information 'Determining Build'
$build = $version_obj | Select-Object -ExpandProperty 'version' | Out-String | ForEach-Object { $_ -replace "`n|`r", '' }
Write-Information "Build is $build"

Write-Information 'Ingesting Indexer Data'
## Get Indexer Data
$indexer_tbl_obj = $indexer_obj | Sort-Object -Property 'name'

## Build Table Fields
Write-Information 'Building Indexer Tables'
### Public Usenet
Write-Information 'Building: Usenet - Public'
$tbl_PubUse = $indexer_tbl_obj | Where-Object { ($_.privacy -eq 'public') -and ($_.protocol -eq 'usenet') } | Select-Object @{Name = 'Indexer'; Expression = $usenet_indexer_name_exp }, @{Name = 'Language'; Expression = { $_.language } }, @{Name = 'Description'; Expression = $indexer_description_exp }
### Private Usenet
Write-Information 'Building: Usenet - Private'
$tbl_PrvUse = $indexer_tbl_obj | Where-Object { ($_.privacy -CIn 'private' -and $_.protocol -eq 'usenet') } | Select-Object @{Name = 'Indexer'; Expression = $usenet_indexer_name_exp }, @{Name = 'Language'; Expression = { $_.language } }, @{Name = 'Description'; Expression = $indexer_description_exp }
### Public Torrents
Write-Information 'Building: Torrents - Public'
$tbl_PubTor = $indexer_tbl_obj | Where-Object { ($_.privacy -eq 'public') -and ($_.protocol -eq 'torrent') } | Select-Object @{Name = 'Indexer'; Expression = $indexer_name_exp }, @{Name = 'Language'; Expression = { $_.language } }, @{Name = 'Description'; Expression = $indexer_description_exp }
### Private Torrents
Write-Information 'Building: Torrents - Private'
$tbl_PrvTor = $indexer_tbl_obj | Where-Object { ($_.privacy -CIn 'private' -and $_.protocol -eq 'torrent') } | Select-Object @{Name = 'Indexer'; Expression = $indexer_name_exp }, @{Name = 'Language'; Expression = { $_.language } }, @{Name = 'Description'; Expression = $indexer_description_exp }

## Convert Data to Markdown Table
$tbl_fmt_PubUse = $tbl_PubUse | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard
$tbl_fmt_PrvUse = $tbl_PrvUse | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard
$tbl_fmt_PubTor = $tbl_PubTor | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard 
$tbl_fmt_PrvTor = $tbl_PrvTor | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard
Write-Information 'Builds Converted to Markdown Tables'

## Page Header Info
$wiki_page_start = $wiki_1newline + "- Supported Trackers and Indexers as of Build ``" + $build + "`` / [Commit: " + $commit + '](' + $wiki_commiturl + $commit + ')'
Write-Information 'Page Header Built'

## Build Page Pieces'
$tbl_fmt_tor = $wiki_1newline + '## Torrents' + $wiki_2newline + '### Public Trackers' + $wiki_2newline + $tbl_fmt_PubTor + $wiki_1newline + '### Private & Semi-Private Trackers' + $wiki_2newline + $tbl_fmt_PrvTor
$tbl_fmt_use = $wiki_1newline + '## Usenet' + $wiki_2newline + '### Public Indexers' + $wiki_2newline + $tbl_fmt_PubUse + $wiki_1newline + '### Private & Semi-Private Indexers' + $wiki_2newline + $tbl_fmt_PrvUse
Write-Information 'Wiki Markdown Tables Built'
$date = [DateTime]::UtcNow.ToString('o')
$mdHeader = 
"`r`n---
title: Prowlarr Supported Indexers
description: Indexers currently named as supported in the current nightly build of Prowlarr. Other indexers may be available via either Generic Newznab or Generic Torznab.
published: true
date: $date
tags: prowlarr, indexers
editor: markdown
dateCreated: $date
---"
$wiki_page_version =
"`r`n---`r`n
- Current `Master` Version | ![Current Master/Stable](https://img.shields.io/badge/dynamic/json?color=f5f5f5&style=flat-square&label=&query=%24.version&url=https://raw.githubusercontent.com/hotio/prowlarr/latest/VERSION.json)
- Current `Develop` Version | ![Current Develop/Beta](https://img.shields.io/badge/dynamic/json?color=f5f5f5&style=flat-square&label=&query=%24.version&url=https://raw.githubusercontent.com/hotio/prowlarr/testing/VERSION.json)
- Current `Nightly` Version | ![Current Nightly/Alpha](https://img.shields.io/badge/dynamic/json?color=f5f5f5&style=flat-square&label=&query=%24.version&url=https://raw.githubusercontent.com/hotio/prowlarr/nightly/VERSION.json)
`r`n---
"
Write-Information 'Wiki Page pieces built'
## Build and Output Page
## We replace because converting to markdown escaped the `\` as `\\` and thus `\\\\` is `\\` in file (due to regex)
$wiki_page_file = ((($mdHeader + $wiki_1newline + $wiki_page_start + $wiki_1newline + $wiki_page_version + $tbl_fmt_tor + $tbl_fmt_use) -replace '\\\\', '\') -replace '---', '---').Trim()
Write-Information 'Wiki Page Built'
$wiki_page_file | Out-File $OutputFile -Encoding $wiki_encoding
Write-Information 'Wiki Page Output'
