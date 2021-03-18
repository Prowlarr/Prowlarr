import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import TagListConnector from 'Components/TagListConnector';
import { icons } from 'Helpers/Props';
import DeleteIndexerModal from 'Indexer/Delete/DeleteIndexerModal';
import EditIndexerModalConnector from 'Settings/Indexers/Indexers/EditIndexerModalConnector';
import titleCase from 'Utilities/String/titleCase';
import translate from 'Utilities/String/translate';
import CapabilitiesLabel from './CapabilitiesLabel';
import IndexerStatusCell from './IndexerStatusCell';
import ProtocolLabel from './ProtocolLabel';
import styles from './IndexerIndexRow.css';

class IndexerIndexRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditIndexerModalOpen: false,
      isDeleteMovieModalOpen: false
    };
  }

  onEditIndexerPress = () => {
    this.setState({ isEditIndexerModalOpen: true });
  }

  onEditIndexerModalClose = () => {
    this.setState({ isEditIndexerModalOpen: false });
  }

  onDeleteMoviePress = () => {
    this.setState({
      isEditIndexerModalOpen: false,
      isDeleteMovieModalOpen: true
    });
  }

  onDeleteMovieModalClose = () => {
    this.setState({ isDeleteMovieModalOpen: false });
  }

  onUseSceneNumberingChange = () => {
    // Mock handler to satisfy `onChange` being required for `CheckInput`.
    //
  }

  //
  // Render

  render() {
    const {
      id,
      name,
      baseUrl,
      enable,
      redirect,
      tags,
      protocol,
      privacy,
      priority,
      status,
      added,
      capabilities,
      columns,
      longDateFormat,
      timeFormat,
      isMovieEditorActive,
      isSelected,
      onSelectedChange
    } = this.props;

    const {
      isEditIndexerModalOpen,
      isDeleteMovieModalOpen
    } = this.state;

    return (
      <>
        {
          columns.map((column) => {
            const {
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (isMovieEditorActive && column.name === 'select') {
              return (
                <VirtualTableSelectCell
                  inputClassName={styles.checkInput}
                  id={id}
                  key={column.name}
                  isSelected={isSelected}
                  isDisabled={false}
                  onSelectedChange={onSelectedChange}
                />
              );
            }

            if (column.name === 'status') {
              return (
                <IndexerStatusCell
                  key={column.name}
                  className={styles[column.name]}
                  enabled={enable}
                  redirect={redirect}
                  status={status}
                  longDateFormat={longDateFormat}
                  timeFormat={timeFormat}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (column.name === 'name') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {name}
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'privacy') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <Label>
                    {titleCase(privacy)}
                  </Label>
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'priority') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {priority}
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'protocol') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <ProtocolLabel
                    protocol={protocol}
                  />
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'capabilities') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <CapabilitiesLabel
                    capabilities={capabilities}
                  />
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'added') {
              return (
                <RelativeDateCellConnector
                  key={column.name}
                  className={styles[column.name]}
                  date={added}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (column.name === 'tags') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <TagListConnector
                    tags={tags}
                  />
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'actions') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <IconButton
                    className={styles.externalLink}
                    name={icons.EXTERNAL_LINK}
                    title={'Website'}
                    to={baseUrl}
                  />

                  <IconButton
                    name={icons.EDIT}
                    title={translate('EditIndexer')}
                    onPress={this.onEditIndexerPress}
                  />
                </VirtualTableRowCell>
              );
            }

            return null;
          })
        }

        <EditIndexerModalConnector
          id={id}
          isOpen={isEditIndexerModalOpen}
          onModalClose={this.onEditIndexerModalClose}
          onDeleteIndexerPress={this.onDeleteMoviePress}
        />

        <DeleteIndexerModal
          isOpen={isDeleteMovieModalOpen}
          indexerId={id}
          onModalClose={this.onDeleteMovieModalClose}
        />
      </>
    );
  }
}

IndexerIndexRow.propTypes = {
  id: PropTypes.number.isRequired,
  baseUrl: PropTypes.string.isRequired,
  protocol: PropTypes.string.isRequired,
  privacy: PropTypes.string.isRequired,
  priority: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  enable: PropTypes.bool.isRequired,
  redirect: PropTypes.bool.isRequired,
  status: PropTypes.object,
  capabilities: PropTypes.object.isRequired,
  added: PropTypes.string.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isMovieEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

IndexerIndexRow.defaultProps = {
  tags: []
};

export default IndexerIndexRow;
