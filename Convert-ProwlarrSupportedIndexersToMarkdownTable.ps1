#Requires -Module FormatMarkdownTable

<#
    .SYNOPSIS
        Name: Convert-ProwlarrSupportedIndexersToMarkdownTable.ps1
        The purpose of this script is to export a markdown table for the wiki of the available indexers
    .DESCRIPTION
        Grabs build number and available indexers from a local or remotely installed Prowlarr instance
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
    [Parameter(Mandatory, Position = 1)]
    [string]$Commit,

    [Parameter(Position = 2)]
    [string]$Build,

    [Parameter(Mandatory, Position = 3)]
    [string]$AppAPIKey,

    [Parameter(Position = 4)]
    [System.IO.FileInfo]
    $OutputFile = ".$([System.IO.Path]::DirectorySeparatorChar)supported-indexers.md",

    [Parameter(Position = 5)]
    [uri]$AppBaseURL = 'http://localhost:9696'
),

## Gather Inputs & Variables
### User Inputs
#### Convert Params to match vars
$app_baseUrl = $AppBaseURL
$app_apikey = $AppAPIKey
####

## Start Variables
### Application Details
$app_api_version = 'v1'
$app_api_path = '/api/'
$app_api_endpoint_version = '/system/status'
$app_api_endpoint_indexer = '/indexer/schema'
$headers = @{'X-Api-Key' = $app_apikey }
#### Github App Info
$gh_app_org = 'Prowlarr'
$gh_app_repo = 'Prowlarr'
### Wiki Details
$wiki_link = 'https://wiki.servarr.com'
$wiki_app_path = '/prowlarr'
$wiki_page = 'supported-indexers'
$wiki_bookmark = '#'
### Page Formating
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
## 
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
$indexer_name_exp = { IF ($_.IndexerUrls) { '[' + $_.name + '](' + ($_.IndexerUrls[0]) + ')' + '{#' + $_.infoLink.Replace($wiki_infolink.ToString(), '') + '}' } Else { $_.name + '{#' + $_.infoLink.Replace($wiki_infolink.ToString(), '') + '}' } }

## Determine Commit
Write-Information "Commit is $commit"

## Determine Version (Build)
Write-Information 'Determining Build'
$build = $version_obj | Select-Object -ExpandProperty 'version' | Out-String | ForEach-Object { $_ -replace "`n|`r", '' }
Write-Information "Build is $build"

Write-Information 'Ingesting Indexer Data'
## Get Indexer Data
$indexer_tbl_obj = $indexer_obj | Sort-Object -Property 'name'

## Build Table Fields
Write-Information 'Building Indexer Tables'
## Public Usenet
Write-Information 'Building: Usenet - Public'
$tbl_PubUse = $indexer_tbl_obj | Where-Object { ($_.privacy -eq 'public') -and ($_.protocol -eq 'usenet') } | `
    Select-Object @{Name = 'Indexer'; Expression = $indexer_name_exp }, `
@{Name = 'Language'; Expression = { $_.language } }, `
@{Name = 'Description'; Expression = { $_.description } }
if ( $null -eq $tbl_PubUse ) { $tbl_PubUse = 'None' }

### Private Usenet
Write-Information 'Building: Usenet - Private'
$tbl_PrvUse = $indexer_tbl_obj | Where-Object { ($_.privacy -CIn 'private' -and $_.protocol -eq 'usenet') } | `
    Select-Object @{Name = 'Indexer'; Expression = $indexer_name_exp }, `
@{Name = 'Language'; Expression = { $_.language } }, `
@{Name = 'Description'; Expression = { $_.description } }

### Public Torrents
Write-Information 'Building: Torrents - Public'
$tbl_PubTor = $indexer_tbl_obj | Where-Object { ($_.privacy -eq 'public') -and ($_.protocol -eq 'torrent') } | `
    Select-Object @{Name = 'Indexer'; Expression = $indexer_name_exp }, `
@{Name = 'Language'; Expression = { $_.language } }, `
@{Name = 'Description'; Expression = { $_.description } }
        
### Private Torrents
Write-Information 'Building: Torrents - Private'
$tbl_PrvTor = $indexer_tbl_obj | Where-Object { ($_.privacy -CIn 'private' -and $_.protocol -eq 'torrent') } | `
    Select-Object @{Name = 'Indexer'; Expression = $indexer_name_exp }, `
@{Name = 'Language'; Expression = { $_.language } }, `
@{Name = 'Description'; Expression = { $_.description } }         

## Convert Data to Markdown Table
$tbl_fmt_PubUse = if ( 'None' -eq $tbl_PubUse ) { $tbl_PubUse | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard }
$tbl_fmt_PrvUse = $tbl_PrvUse | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard
$tbl_fmt_PubTor = $tbl_PubTor | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard 
$tbl_fmt_PrvTor = $tbl_PrvTor | Format-MarkdownTableTableStyle Indexer, Description, Language -HideStandardOutput -ShowMarkdown -DoNotCopyToClipboard
Write-Information 'Builds Converted to Markdown Tables'

## Page Header Info
$wiki_page_start = $wiki_2newline + "- Supported Indexers as of Build ``" + $build + "`` / [Commit: " + $commit + '](' + $wiki_commiturl + $commit + ')'
Write-Information 'Page Header Built'

## Build Page Pieces'
$tbl_fmt_tor = $wiki_1newline + '## Torrents' + $wiki_2newline + '### Public' + $wiki_2newline + $tbl_fmt_PubTor + $wiki_1newline + '### Private' + $wiki_2newline + $tbl_fmt_PrvTor
$tbl_fmt_use = $wiki_1newline + '## Usenet' + $wiki_2newline + '### Public' + $wiki_2newline + $tbl_fmt_PubUse + $wiki_1newline + '### Private' + $wiki_2newline + $tbl_fmt_PrvUse
Write-Information 'Wiki Markdown Tables Built'
$date = [DateTime]::UtcNow.ToString('o')
$mdHeader = 
"---
title: Prowlarr Supported Indexers
description: Indexers currently named as supported in the current nightly build of Prowlarr. Other indexers are available via either Generic Newznab or Generic Torznab.
published: true
date: $date
tags: prowlarr, indexers
editor: markdown
dateCreated: $date
---"
Write-Information 'Wiki Page pieces built'
## Build and output Page
$wiki_page_file = $mdHeader + $wiki_page_start + $wiki_1newline + $tbl_fmt_tor + $tbl_fmt_use
Write-Information 'Wiki Page Built'
$wiki_page_file | Out-File $OutputFile -Encoding $wiki_encoding
Write-Information 'Wiki Page Output'
